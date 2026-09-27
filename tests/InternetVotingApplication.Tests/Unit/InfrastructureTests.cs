using System.Security.Claims;
using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Services.Mail;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InternetVotingApplication.Tests.Unit;

public class InfrastructureTests
{
    [Fact]
    public async Task Security_headers_are_added_to_every_response()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        app.UseSecurityHeaders();
        app.Run(ctx => ctx.Response.WriteAsync("ok"));
        var pipeline = app.Build();
        var context = new DefaultHttpContext();

        await pipeline(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Contains("script-src 'self'", context.Response.Headers.ContentSecurityPolicy.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetUserId_reads_name_identifier_and_fails_loudly_without_it()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")]));
        Assert.Equal(42, principal.GetUserId());

        Assert.Throws<InvalidOperationException>(() => new ClaimsPrincipal(new ClaimsIdentity()).GetUserId());
    }

    [Fact]
    public void Email_validation_accepts_addresses_and_rejects_garbage()
    {
        Assert.True(EmailValidation.IsValidEmail("jan.kowalski@example.com"));
        Assert.False(EmailValidation.IsValidEmail("jan.kowalski"));
        Assert.False(EmailValidation.IsValidEmail(""));
        Assert.False(EmailValidation.IsValidEmail(" "));
    }

    [Fact]
    public void Signing_key_provider_prefers_inline_pem_then_file_then_generates()
    {
        var root = Path.Combine(Path.GetTempPath(), "ivapp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(root);
        var logger = NullLogger.Instance;

        var pem = EcdsaBlockSigner.GeneratePrivateKeyPem();
        var inline = SigningKeyProvider.Create(Options.Create(new SigningOptions { PrivateKeyPem = pem }), environment, logger);
        using var expected = EcdsaBlockSigner.FromPrivateKeyPem(pem);
        Assert.Equal(expected.KeyId, inline.KeyId);

        Assert.Throws<InvalidOperationException>(() =>
            SigningKeyProvider.Create(Options.Create(new SigningOptions { KeyFilePath = "keys/missing.pem" }), environment, logger));

        var generated = SigningKeyProvider.Create(Options.Create(new SigningOptions { KeyFilePath = "keys/dev.pem", AutoGenerateKey = true }), environment, logger);
        Assert.True(File.Exists(Path.Combine(root, "keys", "dev.pem")));

        var reloaded = SigningKeyProvider.Create(Options.Create(new SigningOptions { KeyFilePath = "keys/dev.pem" }), environment, logger);
        Assert.Equal(generated.KeyId, reloaded.KeyId);

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task Pickup_directory_transport_writes_html_files()
    {
        var root = Path.Combine(Path.GetTempPath(), "ivapp-tests", Guid.NewGuid().ToString("N"));
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(root);
        var transport = new PickupDirectoryTransport(Options.Create(new SmtpOptions { PickupDirectory = "mail" }), environment, NullLogger<PickupDirectoryTransport>.Instance);

        await transport.SendAsync(new EmailMessage("jan@example.com", "Temat <1>", "<p>Treść</p>"));

        var file = Assert.Single(Directory.GetFiles(Path.Combine(root, "mail"), "*.html"));
        var html = await File.ReadAllTextAsync(file);
        Assert.Contains("jan@example.com", file, StringComparison.Ordinal);
        Assert.Contains("Temat &lt;1&gt;", html, StringComparison.Ordinal);
        Assert.Contains("<p>Treść</p>", html, StringComparison.Ordinal);

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Mail_transport_factory_chooses_by_configuration()
    {
        static IServiceProvider Build(string? pickup)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Substitute.For<IHostEnvironment>());
            services.AddSingleton(Options.Create(new SmtpOptions { PickupDirectory = pickup }));
            return services.BuildServiceProvider();
        }

        Assert.IsType<PickupDirectoryTransport>(MailTransportFactory.Create(Build("App_Data/mail")));
        Assert.IsType<SmtpEmailSender>(MailTransportFactory.Create(Build(null)));
        Assert.IsType<SmtpEmailSender>(MailTransportFactory.Create(Build("")));
    }

    [Fact]
    public async Task Disabled_smtp_sender_only_logs()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions { Enabled = false }), NullLogger<SmtpEmailSender>.Instance);

        await sender.SendAsync(new EmailMessage("a@b.pl", "T", "B"));
    }

    [Fact]
    public void Options_have_safe_defaults()
    {
        var security = new SecurityOptions();
        Assert.Equal(5, security.MaxFailedLoginAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), security.LockoutDuration);

        var chain = new ChainOptions();
        Assert.Equal(100, chain.AnchorEveryBlocks);
        Assert.Empty(chain.AnchorRecipients);

        var seeding = new SeedingOptions();
        Assert.False(seeding.FirstActivatedUserIsAdmin);
        Assert.False(seeding.SampleData);

        Assert.False(new SigningOptions().AutoGenerateKey);

        var database = new DatabaseOptions();
        Assert.Equal(DatabaseProvider.SqlServer, database.Provider);
        Assert.False(database.FallbackToSqliteWhenUnavailable);
        Assert.Equal(TimeSpan.FromSeconds(3), database.ProbeTimeout);
        Assert.Contains("App_Data", database.SqliteConnectionString, StringComparison.Ordinal);
        Assert.Equal(5, new MailOptions().MaxAttempts);
    }
}

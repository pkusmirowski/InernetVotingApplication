using InternetVotingApplication.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternetVotingApplication.Tests.Integration;

/// <summary>
/// Boots the application exactly as a developer's F5 does (environment Development, real provider selection,
/// no DbContext replacement), but with every path redirected to a temporary directory so the tests never touch
/// the project's own App_Data.
/// </summary>
public sealed class DevelopmentWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ivapp-tests", Guid.NewGuid().ToString("N"));

    public DevelopmentWebApplicationFactory(string provider = "Sqlite", ISqlServerProbe? probe = null, string environment = "Development")
    {
        Provider = provider;
        Probe = probe;
        EnvironmentName = environment;
        Directory.CreateDirectory(_root);
    }

    public string Provider { get; }

    public ISqlServerProbe? Probe { get; }

    public string EnvironmentName { get; }

    public string SqliteFilePath => Path.Combine(_root, "voting-test.db");

    public string MailDirectory => Path.Combine(_root, "mail");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = Provider,
                ["Database:SqliteConnectionString"] = $"Data Source={SqliteFilePath}",
                ["Database:FallbackToSqliteWhenUnavailable"] = "true",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Database:EnsureCreatedOnStartup"] = "false",
                ["ConnectionStrings:InternetVotingDBConnection"] = "Server=127.0.0.1,1;Database=InternetVoting;Trusted_Connection=True;TrustServerCertificate=True;",
                ["Smtp:Enabled"] = "true",
                ["Smtp:PickupDirectory"] = MailDirectory,
                ["Signing:PrivateKeyPem"] = InternetVotingApplication.Blockchain.EcdsaBlockSigner.GeneratePrivateKeyPem(),
                ["Signing:AutoGenerateKey"] = "false",
                // Sample data needs a database; outside Development the SQL Server here is unreachable on purpose.
                ["Seeding:SampleData"] = EnvironmentName == "Development" ? "true" : "false",
                ["Seeding:FirstActivatedUserIsAdmin"] = "true",
                ["Chain:VerificationInterval"] = "01:00:00",
                ["Mail:PollInterval"] = "01:00:00",
                ["RateLimiting:AuthPermitLimit"] = "1000",
            });
        });

        if (Probe != null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISqlServerProbe>();
                services.AddSingleton(Probe);
            });
        }
    }

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_root))
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
                // The SQLite file may still be held briefly; a leftover temp directory is harmless.
            }
        }
    }
}

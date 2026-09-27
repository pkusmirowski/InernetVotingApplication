using System.Net;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.Data;
using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternetVotingApplication.Tests.Integration;

public class DevelopmentModeTests
{
    [Fact]
    public async Task Explicit_sqlite_profile_starts_seeds_sample_data_and_shows_banner()
    {
        using var factory = new DevelopmentWebApplicationFactory(provider: "Sqlite");
        var client = factory.CreateHttpsClient();

        var home = await client.GetAsync(new Uri("/", UriKind.Relative));
        var html = await home.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("Tryb deweloperski", html, StringComparison.Ordinal);
        Assert.Contains("/setup", html, StringComparison.Ordinal);
        Assert.True(File.Exists(factory.SqliteFilePath));

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InternetVotingContext>();
        Assert.Equal(3, await context.DataWyborows.CountAsync());
        var info = scope.ServiceProvider.GetRequiredService<DatabaseInfo>();
        Assert.Equal(DatabaseProvider.Sqlite, info.Provider);
        Assert.Equal(DatabaseSelectionReason.Configured, info.Reason);

        var setup = await client.GetAsync(new Uri("/setup", UriKind.Relative));
        var setupHtml = await setup.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        Assert.Contains("SQLite", setupHtml, StringComparison.Ordinal);
        Assert.Contains(factory.MailDirectory, setupHtml, StringComparison.Ordinal);
        Assert.Contains("Zarejestruj konto", setupHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unreachable_sql_server_in_development_falls_back_and_probes_once()
    {
        var probe = new FakeSqlServerProbe(ok: false, "A network-related or instance-specific error", 2);
        using var factory = new DevelopmentWebApplicationFactory(provider: "SqlServer", probe: probe);
        var client = factory.CreateHttpsClient();

        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Tryb zapasowy", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        Assert.Equal(1, probe.Calls);
        var info = factory.Services.GetRequiredService<DatabaseInfo>();
        Assert.True(info.IsFallback);
        Assert.Equal(DatabaseProvider.Sqlite, info.Provider);

        var setup = await client.GetAsync(new Uri("/setup", UriKind.Relative));
        Assert.Contains("Tryb zapasowy SQLite", await setup.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri("/health", UriKind.Relative))).StatusCode);
    }

    [Fact]
    public async Task Full_voter_flow_works_on_the_sqlite_fallback()
    {
        using var factory = new DevelopmentWebApplicationFactory(provider: "SqlServer", probe: new FakeSqlServerProbe(ok: false));
        var client = factory.CreateHttpsClient();

        var register = await client.PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Imie"] = "Ola",
            ["Nazwisko"] = "Testowa",
            ["Pesel"] = "44051401359",
            ["Email"] = "ola@example.com",
            ["DataUrodzenia"] = "1990-03-04",
            ["Haslo"] = "Secret#Pass1",
            ["ConfirmPassword"] = "Secret#Pass1",
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        await DeliverMailAsync(factory);

        // The background worker may also have dropped an anchor mail for the ended sample election; pick the activation mail.
        var mailFile = Assert.Single(Directory.GetFiles(factory.MailDirectory, "*ola@example.com*.html"));
        var link = System.Text.RegularExpressions.Regex.Match(await File.ReadAllTextAsync(mailFile), "https://[^\"]+/Account/Activation/[0-9a-fA-F-]{36}").Value;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri(link))).StatusCode);

        var login = await client.PostFormAsync("/Account/Login", new Dictionary<string, string> { ["Email"] = "ola@example.com", ["Haslo"] = "Secret#Pass1" });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/Admin/Panel", login.LocationPath());
    }

    [Fact]
    public async Task Outside_development_sql_server_is_kept_without_probing_and_setup_is_hidden()
    {
        var probe = new FakeSqlServerProbe(ok: false);
        using var factory = new DevelopmentWebApplicationFactory(provider: "SqlServer", probe: probe, environment: "Staging");
        var client = factory.CreateHttpsClient();

        using var scope = factory.Services.CreateScope();
        var info = scope.ServiceProvider.GetRequiredService<DatabaseInfo>();
        Assert.Equal(DatabaseProvider.SqlServer, info.Provider);
        Assert.Equal(0, probe.Calls);

        var setup = await client.GetAsync(new Uri("/setup", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, setup.StatusCode);
    }

    /// <summary>The outbox dispatcher runs on a long interval in tests; deliver the queued mail now.</summary>
    private static async Task DeliverMailAsync(DevelopmentWebApplicationFactory factory)
    {
        var dispatcher = factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<InternetVotingApplication.Services.Mail.EmailDispatcher>().Single();
        await dispatcher.ProcessOnceAsync(CancellationToken.None);
    }
}

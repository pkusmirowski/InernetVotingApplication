using System.Net;
using System.Text.RegularExpressions;
using InternetVotingApplication.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace InternetVotingApplication.Tests.Integration;

/// <summary>Administrator path through the real pipeline: first activated account becomes admin, then creates an election.</summary>
public sealed partial class AdminFlowTests : IClassFixture<VotingWebApplicationFactory>
{
    private readonly VotingWebApplicationFactory _factory;

    public AdminFlowTests(VotingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [GeneratedRegex("https://[^\"]+/Account/Activation/[0-9a-fA-F-]{36}")]
    private static partial Regex ActivationLinkRegex();

    [Fact]
    public async Task First_user_becomes_admin_and_manages_an_election()
    {
        // First-account-is-admin only works in the Development environment (it is forced off elsewhere), so this
        // test runs under Development with every convenience that would touch the real project directory disabled.
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seeding:FirstActivatedUserIsAdmin"] = "true",
                ["Seeding:SampleData"] = "false",
                ["Seeding:TestAccounts"] = "false",
                ["Database:Provider"] = "Sqlite",
                ["Database:SqliteConnectionString"] = "Data Source=:memory:",
                ["Database:FallbackToSqliteWhenUnavailable"] = "false",
                ["Smtp:PickupDirectory"] = "",
                ["Signing:AutoGenerateKey"] = "false",
            }));
        });
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        var register = await client.PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Imie"] = "Adam",
            ["Nazwisko"] = "Admin",
            ["Pesel"] = "90010112349",
            ["Email"] = "adam.admin@example.com",
            ["DataUrodzenia"] = "1985-05-05",
            ["Haslo"] = "Secret#Pass1",
            ["ConfirmPassword"] = "Secret#Pass1",
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var link = ActivationLinkRegex().Match(_factory.Emails.Sent.Single(m => m.To == "adam.admin@example.com").HtmlBody).Value;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri(link))).StatusCode);

        var login = await client.PostFormAsync("/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = "adam.admin@example.com",
            ["Haslo"] = "Secret#Pass1",
        });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/Admin/Panel", login.LocationPath());

        var create = await client.PostFormAsync("/Admin/CreateElection", new Dictionary<string, string>
        {
            ["Opis"] = "Wybory admina",
            ["DataRozpoczecia"] = DateTime.Now.AddHours(-1).ToString("yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture),
            ["DataZakonczenia"] = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture),
        });
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

        var elections = await client.GetAsync(new Uri("/Admin/Elections", UriKind.Relative));
        var electionsHtml = await elections.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, elections.StatusCode);
        Assert.Contains("Wybory admina", electionsHtml, StringComparison.Ordinal);

        var electionId = Regex.Match(electionsHtml, "/Admin/VerifyChain/(\\d+)").Groups[1].Value;
        var verify = await client.PostFormAsync("/Admin/Elections", new Dictionary<string, string>(), postUrl: $"/Admin/VerifyChain/{electionId}");
        Assert.Equal(HttpStatusCode.Redirect, verify.StatusCode);

        var audit = await client.GetAsync(new Uri("/Admin/Audit", UriKind.Relative));
        var auditHtml = WebUtility.HtmlDecode(await audit.Content.ReadAsStringAsync());
        Assert.Contains(AuditLog.Actions.Describe(AuditLog.Actions.AdminPromoted), auditHtml, StringComparison.Ordinal);
        Assert.Contains(AuditLog.Actions.Describe(AuditLog.Actions.ElectionCreated), auditHtml, StringComparison.Ordinal);
        Assert.Contains(AuditLog.Actions.Describe(AuditLog.Actions.ChainVerified), auditHtml, StringComparison.Ordinal);

        // A voter (non-admin) is redirected away from admin pages.
        var voterClient = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var voterRegister = await voterClient.PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Imie"] = "Ewa",
            ["Nazwisko"] = "Wyborca",
            ["Pesel"] = "85050522227",
            ["Email"] = "ewa.wyborca@example.com",
            ["DataUrodzenia"] = "1990-01-01",
            ["Haslo"] = "Secret#Pass1",
            ["ConfirmPassword"] = "Secret#Pass1",
        });
        Assert.Equal(HttpStatusCode.OK, voterRegister.StatusCode);
        var voterLink = ActivationLinkRegex().Match(_factory.Emails.Sent.Single(m => m.To == "ewa.wyborca@example.com").HtmlBody).Value;
        await voterClient.GetAsync(new Uri(voterLink));
        var voterLogin = await voterClient.PostFormAsync("/Account/Login", new Dictionary<string, string> { ["Email"] = "ewa.wyborca@example.com", ["Haslo"] = "Secret#Pass1" });
        Assert.Equal("/Election/Dashboard", voterLogin.LocationPath());
        var denied = await voterClient.GetAsync(new Uri("/Admin/Elections", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", denied.LocationPath(), StringComparison.Ordinal);

        // Role changes reach a session that is already open, without logging in again.
        var usersHtml = await (await client.GetAsync(new Uri("/Admin/Users", UriKind.Relative))).Content.ReadAsStringAsync();
        var voterId = Regex.Match(usersHtml, "ewa\\.wyborca@example\\.com[\\s\\S]*?/Admin/SetAdministrator/(\\d+)").Groups[1].Value;
        Assert.NotEmpty(voterId);
        await client.PostFormAsync("/Admin/Users", new Dictionary<string, string> { ["isAdmin"] = "true" }, postUrl: $"/Admin/SetAdministrator/{voterId}");
        Assert.Equal(HttpStatusCode.OK, (await voterClient.GetAsync(new Uri("/Admin/Elections", UriKind.Relative))).StatusCode);
        await client.PostFormAsync("/Admin/Users", new Dictionary<string, string> { ["isAdmin"] = "false" }, postUrl: $"/Admin/SetAdministrator/{voterId}");
        Assert.StartsWith("/Account/AccessDenied", (await voterClient.GetAsync(new Uri("/Admin/Elections", UriKind.Relative))).LocationPath(), StringComparison.Ordinal);

        // A password change keeps the current session and ends the others.
        var secondSession = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        await secondSession.PostFormAsync("/Account/Login", new Dictionary<string, string> { ["Email"] = "ewa.wyborca@example.com", ["Haslo"] = "Secret#Pass1" });
        Assert.Equal(HttpStatusCode.OK, (await secondSession.GetAsync(new Uri("/Election/Dashboard", UriKind.Relative))).StatusCode);
        var change = await voterClient.PostFormAsync("/Account/ChangePassword", new Dictionary<string, string>
        {
            ["Password"] = "Secret#Pass1",
            ["NewPassword"] = "Other#Pass2",
            ["ConfirmNewPassword"] = "Other#Pass2",
        });
        Assert.Equal("/Account/ChangePassword", change.LocationPath());
        Assert.Equal(HttpStatusCode.OK, (await voterClient.GetAsync(new Uri("/Election/Dashboard", UriKind.Relative))).StatusCode);
        Assert.StartsWith("/Account/Login", (await secondSession.GetAsync(new Uri("/Election/Dashboard", UriKind.Relative))).LocationPath(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rate_limiter_returns_429_after_the_permit_limit()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:AuthPermitLimit"] = "2",
                ["RateLimiting:AuthWindow"] = "00:10:00",
            })));
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var form = new Dictionary<string, string> { ["Email"] = "x@y.pl", ["Haslo"] = "bad" };

        var first = await client.PostFormAsync("/Account/Login", form);
        var second = await client.PostFormAsync("/Account/Login", form);
        var third = await client.PostFormAsync("/Account/Login", form);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}

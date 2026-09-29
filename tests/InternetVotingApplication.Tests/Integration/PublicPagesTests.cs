using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace InternetVotingApplication.Tests.Integration;

public sealed class PublicPagesTests : IClassFixture<VotingWebApplicationFactory>
{
    private readonly VotingWebApplicationFactory _factory;

    public PublicPagesTests(VotingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Privacy")]
    [InlineData("/Home/Contact")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    [InlineData("/Account/PasswordRecovery")]
    [InlineData("/Account/Search")]
    public async Task Public_pages_return_200_with_security_headers(string url)
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("Content-Security-Policy", response.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task Pages_show_no_placeholder_contact_data_or_external_fonts()
    {
        var client = _factory.CreateHttpsClient();

        foreach (var url in new[] { "/", "/Home/Contact", "/Home/Privacy" })
        {
            var html = WebUtility.HtmlDecode(await client.GetStringAsync(new Uri(url, UriKind.Relative)));
            Assert.DoesNotContain("Przykładowa", html, StringComparison.Ordinal);
            Assert.DoesNotContain("twojadomena", html, StringComparison.Ordinal);
            Assert.DoesNotContain("000 000 000", html, StringComparison.Ordinal);
            Assert.DoesNotContain("fonts.googleapis", html, StringComparison.Ordinal);
            Assert.Contains("Głosowanie internetowe", html, StringComparison.Ordinal);
        }

        var contact = WebUtility.HtmlDecode(await client.GetStringAsync(new Uri("/Home/Contact", UriKind.Relative)));
        Assert.Contains("nie zostały jeszcze skonfigurowane", contact, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Configured_contact_email_appears_in_footer_and_on_contact_page()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["App:ContactEmail"] = "kontakt@glosowanie.pl" })));
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var home = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        var contact = await client.GetStringAsync(new Uri("/Home/Contact", UriKind.Relative));

        Assert.Contains("mailto:kontakt@glosowanie.pl", home, StringComparison.Ordinal);
        Assert.Contains("mailto:kontakt@glosowanie.pl", contact, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/Election/Dashboard")]
    [InlineData("/Election/Voting/1")]
    [InlineData("/Account/ChangePassword")]
    [InlineData("/Admin/Panel")]
    [InlineData("/Admin/AddCandidate")]
    public async Task Protected_pages_redirect_anonymous_users_to_login(string url)
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.LocationPath(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected()
    {
        var client = _factory.CreateHttpsClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["Email"] = "a@b.pl", ["Haslo"] = "x" });

        var response = await client.PostAsync(new Uri("/Account/Login", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_get_parameters_does_not_sign_in()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.GetAsync(new Uri("/Account/Login?Email=a@b.pl&Haslo=x", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];
        Assert.DoesNotContain(cookies, c => c.StartsWith("InternetVoting.Auth=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unknown_page_renders_custom_404()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.GetAsync(new Uri("/nie/ma/takiej/strony", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("nie istnieje", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Registration_errors_are_shown_in_polish()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Imie"] = new string('a', 51),
            ["Nazwisko"] = "Testowa",
            ["Pesel"] = "44051401359",
            ["Email"] = "zla.data@example.com",
            ["DataUrodzenia"] = "to nie jest data",
            ["Haslo"] = "Secret#Pass1",
            ["ConfirmPassword"] = "Secret#Pass1",
        });

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("To pole może mieć najwyżej 50 znaków.", html, StringComparison.Ordinal);
        Assert.Contains("jest nieprawidłowa", html, StringComparison.Ordinal);
        Assert.DoesNotContain("The value", html, StringComparison.Ordinal);
        Assert.DoesNotContain("The field", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_for_unknown_hash_reports_not_found()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.GetAsync(new Uri("/Account/Search?hash=" + new string('A', 64), UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Nie znaleziono głosu", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}

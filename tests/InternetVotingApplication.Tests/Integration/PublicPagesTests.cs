using System.Net;

namespace InternetVotingApplication.Tests.Integration
{
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
        public async Task Search_for_unknown_hash_reports_not_found()
        {
            var client = _factory.CreateHttpsClient();

            var response = await client.GetAsync(new Uri("/Account/Search?hash=" + new string('A', 64), UriKind.Relative));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Nie znaleziono głosu", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
    }
}

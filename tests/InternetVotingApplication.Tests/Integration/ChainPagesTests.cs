using InternetVotingApplication.Models;
using System.Net;
using System.Text.Json;

namespace InternetVotingApplication.Tests.Integration
{
    public sealed class ChainPagesTests : IClassFixture<VotingWebApplicationFactory>
    {
        private readonly VotingWebApplicationFactory _factory;

        public ChainPagesTests(VotingWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Health_endpoint_reports_healthy()
        {
            var response = await _factory.CreateHttpsClient().GetAsync(new Uri("/health", UriKind.Relative));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Chain_page_and_export_are_public_and_consistent()
        {
            int electionId;
            using (var context = _factory.CreateContext())
            {
                var election = new DataWyborow { Opis = "Wybory publiczne", DataRozpoczecia = DateTime.Now.AddDays(-2), DataZakonczenia = DateTime.Now.AddDays(-1) };
                context.Add(election);
                await context.SaveChangesAsync();
                electionId = election.Id;
            }

            var client = _factory.CreateHttpsClient();

            var page = await client.GetAsync(new Uri($"/Election/Chain/{electionId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync();
            Assert.Contains("BEGIN PUBLIC KEY", html, StringComparison.Ordinal);
            Assert.Contains("Wyniki", html, StringComparison.Ordinal);

            var export = await client.GetAsync(new Uri($"/Election/Export/{electionId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, export.StatusCode);
            Assert.Equal("application/json", export.Content.Headers.ContentType!.MediaType);
            using var json = JsonDocument.Parse(await export.Content.ReadAsStringAsync());
            Assert.Equal("internet-voting-chain/v2", json.RootElement.GetProperty("format").GetString());
            Assert.Equal(electionId, json.RootElement.GetProperty("election").GetProperty("id").GetInt32());
            Assert.Equal(0, json.RootElement.GetProperty("blocks").GetArrayLength());

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(new Uri("/Election/Chain/99999", UriKind.Relative))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(new Uri("/Election/Export/99999", UriKind.Relative))).StatusCode);
        }

        [Theory]
        [InlineData("/Admin/Elections")]
        [InlineData("/Admin/Audit")]
        public async Task Admin_chain_pages_require_login(string url)
        {
            var response = await _factory.CreateHttpsClient().GetAsync(new Uri(url, UriKind.Relative));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/Account/Login", response.LocationPath(), StringComparison.Ordinal);
        }
    }
}

using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;

namespace InternetVotingApplication.Tests.Integration
{
    public sealed partial class VotingFlowTests : IClassFixture<VotingWebApplicationFactory>
    {
        private readonly VotingWebApplicationFactory _factory;

        public VotingFlowTests(VotingWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [GeneratedRegex("https://[^\"]+/Account/Activation/[0-9a-fA-F-]{36}")]
        private static partial Regex ActivationLinkRegex();

        [GeneratedRegex("[0-9A-F]{64}")]
        private static partial Regex HashRegex();

        [Fact]
        public async Task Register_activate_login_vote_and_verify_receipt()
        {
            var client = _factory.CreateHttpsClient();

            // Register
            var register = await client.PostFormAsync("/Account/Register", new Dictionary<string, string>
            {
                ["Imie"] = "Ewa",
                ["Nazwisko"] = "Testowa",
                ["Pesel"] = "44051401359",
                ["Email"] = "ewa.testowa@example.com",
                ["DataUrodzenia"] = "1990-03-04",
                ["Haslo"] = "Secret#Pass1",
                ["ConfirmPassword"] = "Secret#Pass1",
            });
            Assert.Equal(HttpStatusCode.OK, register.StatusCode);
            Assert.Contains("link aktywacyjny", await register.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            // Activate through the link from the captured e-mail
            var activationMail = _factory.Emails.Sent.Single(m => m.To == "ewa.testowa@example.com" && m.Subject.Contains("Aktywacja", StringComparison.Ordinal));
            var link = ActivationLinkRegex().Match(activationMail.HtmlBody).Value;
            var activation = await client.GetAsync(new Uri(link));
            Assert.Equal(HttpStatusCode.OK, activation.StatusCode);
            Assert.Contains("Konto zostało aktywowane", await activation.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            // Seed an ongoing election with two candidates
            int electionId;
            int candidateId;
            using (var context = _factory.CreateContext())
            {
                var election = new DataWyborow { Opis = "Wybory integracyjne", DataRozpoczecia = DateTime.Now.AddHours(-1), DataZakonczenia = DateTime.Now.AddHours(1) };
                var first = new Kandydat { Imie = "Pierwszy", Nazwisko = "Kandydat", IdWyboryNavigation = election };
                var second = new Kandydat { Imie = "Drugi", Nazwisko = "Kandydat", IdWyboryNavigation = election };
                context.AddRange(election, first, second);
                await context.SaveChangesAsync();
                electionId = election.Id;
                candidateId = second.Id;
            }

            // Login
            var login = await client.PostFormAsync("/Account/Login", new Dictionary<string, string>
            {
                ["Email"] = "ewa.testowa@example.com",
                ["Haslo"] = "Secret#Pass1",
            });
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            Assert.Equal("/Election/Dashboard", login.LocationPath());

            var dashboard = await client.GetAsync(new Uri("/Election/Dashboard", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
            Assert.Contains("Wybory integracyjne", await dashboard.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            // Vote
            var vote = await client.PostFormAsync($"/Election/Voting/{electionId}", new Dictionary<string, string>
            {
                ["ElectionId"] = electionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["SelectedCandidateId"] = candidateId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }, postUrl: "/Election/Vote");
            Assert.Equal(HttpStatusCode.Redirect, vote.StatusCode);
            Assert.Equal("/Election/Voted", vote.LocationPath());

            var receipt = await client.GetAsync(new Uri("/Election/Voted", UriKind.Relative));
            var receiptHtml = await receipt.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);
            var hash = HashRegex().Match(receiptHtml).Value;
            Assert.Equal(64, hash.Length);

            // Voting again is not possible
            var again = await client.GetAsync(new Uri($"/Election/Voting/{electionId}", UriKind.Relative));
            Assert.Equal(HttpStatusCode.Redirect, again.StatusCode);
            Assert.Equal($"/Election/ElectionResult/{electionId}", again.LocationPath());

            // The receipt can be verified publicly
            var anonymous = _factory.CreateHttpsClient();
            var search = await anonymous.GetAsync(new Uri("/Account/Search?hash=" + hash, UriKind.Relative));
            var searchHtml = await search.Content.ReadAsStringAsync();
            Assert.Contains("nienaruszony", searchHtml, StringComparison.Ordinal);
            Assert.Contains("Drugi Kandydat", searchHtml, StringComparison.Ordinal);

            // The block is persisted and the chain verifies
            using (var context = _factory.CreateContext())
            {
                var block = await context.GlosowanieWyborczes.SingleAsync(g => g.Hash == hash);
                Assert.Equal(candidateId, block.IdKandydat);
                Assert.Equal(1, await context.GlosUzytkownikas.CountAsync(g => g.IdWybory == electionId));
            }

            var receiptMail = _factory.Emails.Sent.Single(m => m.Subject.Contains("Potwierdzenie", StringComparison.Ordinal));
            Assert.Contains(hash, receiptMail.HtmlBody, StringComparison.Ordinal);

            // AnchorEveryBlocks=1 in the test configuration: the first block triggers an anchor to the committee.
            var anchorMail = _factory.Emails.Sent.Single(m => m.To == "komisja@example.com");
            Assert.Contains(hash, anchorMail.HtmlBody, StringComparison.Ordinal);

            // The public export verifies with the independent tool.
            var exportJson = await anonymous.GetStringAsync(new Uri($"/Election/Export/{electionId}", UriKind.Relative));
            var report = ChainVerifier.Verifier.Verify(ChainVerifier.Verifier.Parse(exportJson));
            Assert.True(report.IsValid, string.Join("; ", report.Errors));
            Assert.Equal(1, report.BlockCount);
            Assert.Equal(1, report.AnchorsMatched);
            Assert.Equal(hash, report.HeadHash);
        }
    }
}

using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace InternetVotingApplication.Tests.Services
{
    public sealed class ChainServiceTests : IDisposable
    {
        private readonly SqliteDatabase _db = new();
        private readonly FakeEmailSender _email = new();
        private readonly FakeTimeProvider _clock = TestData.Clock();

        private async Task<(DataWyborow Election, List<Uzytkownik> Users, Kandydat Candidate)> SeedWithVotesAsync(InternetVotingContext context, int votes)
        {
            var election = TestData.OngoingElection();
            var candidate = new Kandydat { Imie = "Adam", Nazwisko = "A", IdWyboryNavigation = election };
            var users = new List<Uzytkownik>();
            var peselBases = new[] { "9001011234", "8505052222", "7712123333", "9911114444", "6606066666", "7507077777" };
            for (int i = 0; i < votes; i++)
            {
                users.Add(TestData.User(email: $"u{i}@example.com", pesel: Pesel(peselBases[i])));
            }

            context.AddRange(election, candidate);
            context.AddRange(users);
            await context.SaveChangesAsync();

            var voting = TestData.Election(context, _clock, _email);
            foreach (var user in users)
            {
                Assert.Equal(VoteStatus.Success, (await voting.CastVoteAsync(user.Id, election.Id, candidate.Id)).Status);
            }

            return (election, users, candidate);
        }

        private static string Pesel(string base10)
        {
            int[] w = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];
            var sum = base10.Select((c, i) => (c - '0') * w[i]).Sum();
            return base10 + ((10 - (sum % 10)) % 10).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        [Fact]
        public async Task Verification_is_stored_and_reports_head_mismatch()
        {
            using var context = _db.CreateContext();
            var (election, _, _) = await SeedWithVotesAsync(context, 3);
            var chain = TestData.Chain(context, _clock, _email);

            var ok = await chain.VerifyAndStoreAsync(election.Id, "Manual", actorUserId: 1);
            Assert.True(ok.IsValid);
            Assert.Equal(3, ok.BlockCount);
            var last = await chain.GetLastVerificationAsync(election.Id);
            Assert.NotNull(last);
            Assert.True(last.IsValid);
            Assert.Equal("Manual", last.Trigger);
            Assert.Contains(context.DziennikAudytu, a => a.Akcja == "ChainVerified" && a.IdUzytkownik == 1);

            // Tamper with the stored head state only.
            var row = await context.DataWyborows.SingleAsync(e => e.Id == election.Id);
            row.LiczbaBlokow = 2;
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var bad = await chain.VerifyAndStoreAsync(election.Id, "Background");
            Assert.False(bad.IsValid);
            Assert.True(bad.HashesValid);
            var stored = await chain.GetLastVerificationAsync(election.Id);
            Assert.NotNull(stored);
            Assert.False(stored.IsValid);
            Assert.Contains("Stan głowy", stored.Details, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Anchor_is_signed_recorded_and_mailed()
        {
            using var context = _db.CreateContext();
            var (election, _, _) = await SeedWithVotesAsync(context, 2);
            var chain = TestData.Chain(context, _clock, _email, TestData.ChainOptions(recipients: ["komisja@example.com", "obserwator@example.com"]));
            _email.Sent.Clear();

            var anchor = await chain.PublishAnchorAsync(election.Id, ChainService.ReasonManual, actorUserId: 5);

            Assert.NotNull(anchor);
            Assert.Equal(2, anchor.BlockCount);
            var head = await context.DataWyborows.AsNoTracking().Where(e => e.Id == election.Id).Select(e => e.HashGlowy).SingleAsync();
            Assert.Equal(head, anchor.HeadHash);
            Assert.True(TestData.Signer.Verify(
                Blockchain.BlockChainHelper.AnchorData(election.Id, 2, head, anchor.Date), anchor.Signature));

            Assert.Equal(2, _email.Sent.Count);
            Assert.All(_email.Sent, m => Assert.Contains(head!, m.HtmlBody, StringComparison.Ordinal));
            Assert.Single(context.Kotwice);
            Assert.True(await chain.HasFinalAnchorAsync(election.Id) == false);
            Assert.Contains(context.DziennikAudytu, a => a.Akcja == "AnchorPublished" && a.IdUzytkownik == 5);
            Assert.Null(await chain.PublishAnchorAsync(999, ChainService.ReasonManual));
        }

        [Fact]
        public async Task Periodic_anchor_is_published_every_n_blocks()
        {
            using var context = _db.CreateContext();
            var election = TestData.OngoingElection();
            var candidate = new Kandydat { Imie = "Adam", Nazwisko = "A", IdWyboryNavigation = election };
            var u1 = TestData.User(email: "a@example.com", pesel: Pesel("9001011234"));
            var u2 = TestData.User(email: "b@example.com", pesel: Pesel("8505052222"));
            var u3 = TestData.User(email: "c@example.com", pesel: Pesel("7712123333"));
            context.AddRange(election, candidate, u1, u2, u3);
            await context.SaveChangesAsync();
            var voting = TestData.Election(context, _clock, _email, TestData.ChainOptions(anchorEveryBlocks: 2));

            await voting.CastVoteAsync(u1.Id, election.Id, candidate.Id);
            Assert.Empty(context.Kotwice);
            await voting.CastVoteAsync(u2.Id, election.Id, candidate.Id);
            var anchor = Assert.Single(context.Kotwice);
            Assert.Equal(ChainService.ReasonPeriodic, anchor.Powod);
            Assert.Equal(2, anchor.LiczbaBlokow);
            await voting.CastVoteAsync(u3.Id, election.Id, candidate.Id);
            Assert.Single(context.Kotwice);
        }

        [Fact]
        public async Task Export_contains_everything_needed_for_independent_verification()
        {
            using var context = _db.CreateContext();
            var (election, _, candidate) = await SeedWithVotesAsync(context, 3);
            var chain = TestData.Chain(context, _clock, _email);
            await chain.PublishAnchorAsync(election.Id, ChainService.ReasonManual);

            var export = await chain.ExportAsync(election.Id);

            Assert.NotNull(export);
            Assert.Equal("internet-voting-chain/v2", export.Format);
            Assert.Equal(TestData.Signer.KeyId, export.KeyId);
            Assert.Equal(TestData.Signer.PublicKeyPem, export.PublicKeyPem);
            Assert.Equal(3, export.Blocks.Count);
            Assert.Null(export.Blocks[0].PreviousHash);
            Assert.Equal(export.Blocks[0].Hash, export.Blocks[1].PreviousHash);
            Assert.Equal(export.Election.HeadHash, export.Blocks[2].Hash);
            Assert.Single(export.Candidates, c => c.Id == candidate.Id);
            Assert.Single(export.Anchors);
            Assert.Null(await chain.ExportAsync(999));
        }

        [Fact]
        public async Task Admin_overview_and_active_election_selection()
        {
            using var context = _db.CreateContext();
            var (election, users, _) = await SeedWithVotesAsync(context, 2);
            context.DataWyborows.Add(new DataWyborow { Opis = "Przyszłe", DataRozpoczecia = TestData.Now.AddDays(5), DataZakonczenia = TestData.Now.AddDays(6) });
            context.DataWyborows.Add(new DataWyborow { Opis = "Dawne", DataRozpoczecia = TestData.Now.AddDays(-60), DataZakonczenia = TestData.Now.AddDays(-50) });
            await context.SaveChangesAsync();
            var chain = TestData.Chain(context, _clock, _email);

            var active = await chain.GetActiveElectionIdsAsync();
            Assert.Equal([election.Id], active);

            var overview = await chain.GetAdminOverviewAsync();
            Assert.Equal(3, overview.Count);
            var row = overview.Single(o => o.Id == election.Id);
            Assert.Equal(2, row.BlockCount);
            Assert.Equal(users.Count, row.Participants);
            Assert.Equal(1, row.CandidateCount);
            Assert.Null(row.LastVerification);
            Assert.False(row.HasFinalAnchor);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}

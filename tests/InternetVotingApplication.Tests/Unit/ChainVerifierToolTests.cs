using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using System.Text.Json;

namespace InternetVotingApplication.Tests.Unit
{
    /// <summary>The standalone verifier must accept what the application exports and reject tampering.</summary>
    public sealed class ChainVerifierToolTests : IDisposable
    {
        private readonly SqliteDatabase _db = new();
        private readonly FakeEmailSender _email = new();
        private readonly Microsoft.Extensions.Time.Testing.FakeTimeProvider _clock = TestData.Clock();

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private async Task<string> ExportJsonAsync(Action<InternetVotingContext>? tamper = null)
        {
            using var context = _db.CreateContext();
            var election = TestData.OngoingElection();
            var a = new Kandydat { Imie = "Adam", Nazwisko = "A", IdWyboryNavigation = election };
            var b = new Kandydat { Imie = "Beata", Nazwisko = "B", IdWyboryNavigation = election };
            var u1 = TestData.User(email: "a@example.com", pesel: "90010112349");
            var u2 = TestData.User(email: "b@example.com", pesel: "85050522227");
            var u3 = TestData.User(email: "c@example.com", pesel: "77121233330");
            context.AddRange(election, a, b, u1, u2, u3);
            await context.SaveChangesAsync();

            var voting = TestData.Election(context, _clock, _email);
            await voting.CastVoteAsync(u1.Id, election.Id, a.Id);
            await voting.CastVoteAsync(u2.Id, election.Id, b.Id);
            await voting.CastVoteAsync(u3.Id, election.Id, a.Id);
            var chain = TestData.Chain(context, _clock, _email);
            await chain.PublishAnchorAsync(election.Id, ChainService.ReasonManual);

            tamper?.Invoke(context);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var export = await chain.ExportAsync(election.Id);
            return JsonSerializer.Serialize(export, Json);
        }

        [Fact]
        public async Task Accepts_a_genuine_export_and_tallies_votes()
        {
            var json = await ExportJsonAsync();

            var report = ChainVerifier.Verifier.Verify(ChainVerifier.Verifier.Parse(json));

            Assert.True(report.IsValid, string.Join("; ", report.Errors));
            Assert.Equal(3, report.BlockCount);
            Assert.Equal(1, report.AnchorsMatched);
            Assert.Equal(2, report.Tally["Adam A"]);
            Assert.Equal(1, report.Tally["Beata B"]);
        }

        [Fact]
        public async Task Detects_a_flipped_vote()
        {
            var json = await ExportJsonAsync(ctx =>
            {
                var block = ctx.GlosowanieWyborczes.Single(g => g.Indeks == 1);
                block.IdKandydat = ctx.Kandydats.Single(k => k.Imie == "Adam").Id;
            });

            var report = ChainVerifier.Verifier.Verify(ChainVerifier.Verifier.Parse(json));

            Assert.False(report.IsValid);
            Assert.Contains(report.Errors, e => e.StartsWith("Blok 1: hash", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Detects_a_rewritten_history_through_the_anchor()
        {
            var json = await ExportJsonAsync(ctx =>
            {
                // Rewrite block 1 and recompute hashes and signatures of the rest with the real key:
                // an insider with the database AND the key. Only the anchor sent outside can reveal this.
                var blocks = ctx.GlosowanieWyborczes.OrderBy(g => g.Indeks).ToList();
                blocks[1].IdKandydat = ctx.Kandydats.Single(k => k.Imie == "Adam").Id;
                string? previous = blocks[0].Hash;
                for (int i = 1; i < blocks.Count; i++)
                {
                    blocks[i].Hash = Blockchain.BlockHelper.ComputeHash(blocks[i], previous);
                    blocks[i].Podpis = TestData.Signer.Sign(blocks[i].Hash);
                    previous = blocks[i].Hash;
                }

                var election = ctx.DataWyborows.Single();
                election.HashGlowy = previous;
            });

            var report = ChainVerifier.Verifier.Verify(ChainVerifier.Verifier.Parse(json));

            Assert.False(report.IsValid);
            Assert.Contains(report.Errors, e => e.Contains("historia została przepisana", StringComparison.Ordinal));
            Assert.Equal(0, report.AnchorsMatched);
        }

        [Fact]
        public async Task Detects_a_truncated_chain_through_the_anchor()
        {
            var json = await ExportJsonAsync(ctx =>
            {
                var last = ctx.GlosowanieWyborczes.OrderByDescending(g => g.Indeks).First();
                ctx.GlosowanieWyborczes.Remove(last);
                var election = ctx.DataWyborows.Single();
                election.LiczbaBlokow = 2;
                election.HashGlowy = ctx.GlosowanieWyborczes.Single(g => g.Indeks == 1).Hash;
            });

            var report = ChainVerifier.Verifier.Verify(ChainVerifier.Verifier.Parse(json));

            Assert.False(report.IsValid);
            Assert.Contains(report.Errors, e => e.Contains("historia została skrócona", StringComparison.Ordinal));
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}

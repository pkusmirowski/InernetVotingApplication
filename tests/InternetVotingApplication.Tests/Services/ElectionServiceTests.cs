using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace InternetVotingApplication.Tests.Services
{
    public sealed class ElectionServiceTests : IDisposable
    {
        private readonly SqliteDatabase _db = new();
        private readonly FakeEmailSender _email = new();
        private readonly FakeTimeProvider _clock = TestData.Clock();

        private ElectionService CreateService(InternetVotingContext context)
        {
            return new ElectionService(context, _email, _clock, TestData.Logger<ElectionService>());
        }

        private async Task<(Uzytkownik User, DataWyborow Election, Kandydat A, Kandydat B)> SeedAsync(InternetVotingContext context)
        {
            var user = TestData.User();
            var election = TestData.OngoingElection();
            var a = new Kandydat { Imie = "Adam", Nazwisko = "A", IdWyboryNavigation = election };
            var b = new Kandydat { Imie = "Beata", Nazwisko = "B", IdWyboryNavigation = election };
            context.AddRange(user, election, a, b);
            await context.SaveChangesAsync();
            return (user, election, a, b);
        }

        [Fact]
        public async Task Casting_a_vote_appends_a_valid_block_and_records_participation()
        {
            using var context = _db.CreateContext();
            var (user, election, a, _) = await SeedAsync(context);
            var service = CreateService(context);

            var outcome = await service.CastVoteAsync(user.Id, election.Id, a.Id);

            Assert.Equal(VoteStatus.Success, outcome.Status);
            Assert.Matches("^[0-9A-F]{64}$", outcome.Hash);

            var block = Assert.Single(context.GlosowanieWyborczes);
            Assert.Equal(0, block.Indeks);
            Assert.Null(block.IdPoprzednie);
            Assert.Equal(a.Id, block.IdKandydat);
            Assert.Equal(outcome.Hash, block.Hash);

            var participation = Assert.Single(context.GlosUzytkownikas);
            Assert.Equal(user.Id, participation.IdUzytkownik);

            var mail = Assert.Single(_email.Sent);
            Assert.Contains(outcome.Hash!, mail.HtmlBody, StringComparison.Ordinal);
            Assert.True((await service.VerifyChainAsync(election.Id)).IsValid);
        }

        [Fact]
        public async Task Second_vote_of_the_same_user_is_rejected()
        {
            using var context = _db.CreateContext();
            var (user, election, a, b) = await SeedAsync(context);
            var service = CreateService(context);

            await service.CastVoteAsync(user.Id, election.Id, a.Id);
            var second = await service.CastVoteAsync(user.Id, election.Id, b.Id);

            Assert.Equal(VoteStatus.AlreadyVoted, second.Status);
            Assert.Equal(1, context.GlosowanieWyborczes.Count());
            Assert.True(await service.HasVotedAsync(user.Id, election.Id));
        }

        [Fact]
        public async Task Candidate_from_another_election_is_rejected()
        {
            using var context = _db.CreateContext();
            var (user, election, _, _) = await SeedAsync(context);
            var other = TestData.OngoingElection("Inne wybory");
            var stranger = new Kandydat { Imie = "Celina", Nazwisko = "C", IdWyboryNavigation = other };
            context.AddRange(other, stranger);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var outcome = await service.CastVoteAsync(user.Id, election.Id, stranger.Id);

            Assert.Equal(VoteStatus.CandidateNotInElection, outcome.Status);
            Assert.Empty(context.GlosowanieWyborczes);
        }

        [Fact]
        public async Task Votes_outside_the_voting_window_are_rejected()
        {
            using var context = _db.CreateContext();
            var (user, election, a, _) = await SeedAsync(context);
            var clock = new FakeTimeProvider(new DateTimeOffset(election.DataRozpoczecia.AddHours(-1), TimeSpan.Zero));
            var service = new ElectionService(context, _email, clock, TestData.Logger<ElectionService>());

            Assert.Equal(VoteStatus.ElectionNotStarted, (await service.CastVoteAsync(user.Id, election.Id, a.Id)).Status);

            clock.SetUtcNow(new DateTimeOffset(election.DataZakonczenia.AddHours(1), TimeSpan.Zero));
            Assert.Equal(VoteStatus.ElectionEnded, (await service.CastVoteAsync(user.Id, election.Id, a.Id)).Status);

            Assert.Equal(VoteStatus.ElectionNotFound, (await service.CastVoteAsync(user.Id, 999, a.Id)).Status);
            Assert.Empty(context.GlosowanieWyborczes);
        }

        [Fact]
        public async Task Chain_links_successive_votes_and_detects_tampering()
        {
            using var context = _db.CreateContext();
            var (user, election, a, b) = await SeedAsync(context);
            var second = TestData.User(email: "ewa@example.com", pesel: "02070803628");
            var third = TestData.User(email: "olaf@example.com", pesel: "00000000000");
            context.AddRange(second, third);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CastVoteAsync(user.Id, election.Id, a.Id);
            await service.CastVoteAsync(second.Id, election.Id, b.Id);
            await service.CastVoteAsync(third.Id, election.Id, a.Id);

            var blocks = await context.GlosowanieWyborczes.OrderBy(g => g.Indeks).ToListAsync();
            Assert.Equal([0, 1, 2], blocks.Select(x => x.Indeks));
            Assert.Equal(blocks[0].Id, blocks[1].IdPoprzednie);
            Assert.Equal(blocks[1].Id, blocks[2].IdPoprzednie);
            Assert.True((await service.VerifyChainAsync(election.Id)).IsValid);

            // Someone with database access flips a vote.
            blocks[1].IdKandydat = a.Id;
            await context.SaveChangesAsync();

            var verification = await service.VerifyChainAsync(election.Id);
            Assert.False(verification.IsValid);
            Assert.Contains(blocks[1].Id, verification.InvalidBlockIds);

            var fourth = TestData.User(email: "zofia@example.com", pesel: "90010112349");
            context.Uzytkowniks.Add(fourth);
            await context.SaveChangesAsync();
            Assert.Equal(VoteStatus.ChainCorrupted, (await service.CastVoteAsync(fourth.Id, election.Id, a.Id)).Status);
        }

        [Fact]
        public async Task Results_include_all_candidates_with_correct_percentages()
        {
            using var context = _db.CreateContext();
            var (user, election, a, b) = await SeedAsync(context);
            var second = TestData.User(email: "ewa@example.com", pesel: "02070803628");
            var third = TestData.User(email: "olaf@example.com", pesel: "00000000000");
            var idle = new Kandydat { Imie = "Nikt", Nazwisko = "N", IdWybory = election.Id };
            context.AddRange(second, third, idle);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CastVoteAsync(user.Id, election.Id, a.Id);
            await service.CastVoteAsync(second.Id, election.Id, a.Id);
            await service.CastVoteAsync(third.Id, election.Id, b.Id);

            var results = await service.GetResultsAsync(election.Id);

            Assert.NotNull(results);
            Assert.Equal(3, results.TotalVotes);
            Assert.Equal(3, results.Rows.Count);
            Assert.Equal(3, results.BlockCount);
            Assert.True(results.ChainValid);
            var rowA = results.Rows.Single(r => r.IdKandydat == a.Id);
            var rowB = results.Rows.Single(r => r.IdKandydat == b.Id);
            var rowIdle = results.Rows.Single(r => r.IdKandydat == idle.Id);
            Assert.Equal(2, rowA.CountedVotes);
            Assert.Equal(66.67, rowA.CountedVotesPercentage);
            Assert.Equal(33.33, rowB.CountedVotesPercentage);
            Assert.Equal(0, rowIdle.CountedVotes);
            Assert.Equal(rowA.IdKandydat, results.Rows[0].IdKandydat);
        }

        [Fact]
        public async Task Search_finds_vote_by_hash_case_insensitively()
        {
            using var context = _db.CreateContext();
            var (user, election, a, _) = await SeedAsync(context);
            var service = CreateService(context);
            var outcome = await service.CastVoteAsync(user.Id, election.Id, a.Id);

            var found = await service.SearchVoteAsync(" " + outcome.Hash!.ToLowerInvariant() + " ");
            Assert.True(found.Searched);
            Assert.True(found.Found);
            Assert.True(found.ChainValid);
            Assert.Equal("Adam", found.CandidateName);
            Assert.Equal(election.Opis, found.ElectionName);

            var missing = await service.SearchVoteAsync(HashHelper.Hash("nope"));
            Assert.True(missing.Searched);
            Assert.False(missing.Found);

            Assert.False((await service.SearchVoteAsync("")).Searched);
        }

        [Fact]
        public async Task Election_list_reports_status_and_participation()
        {
            using var context = _db.CreateContext();
            var (user, election, a, _) = await SeedAsync(context);
            context.DataWyborows.Add(new DataWyborow { Opis = "Przyszłe", DataRozpoczecia = TestData.Now.AddDays(5), DataZakonczenia = TestData.Now.AddDays(6) });
            context.DataWyborows.Add(new DataWyborow { Opis = "Minione", DataRozpoczecia = TestData.Now.AddDays(-6), DataZakonczenia = TestData.Now.AddDays(-5) });
            await context.SaveChangesAsync();
            var service = CreateService(context);
            await service.CastVoteAsync(user.Id, election.Id, a.Id);

            var list = await service.GetElectionListAsync(user.Id);

            Assert.Equal(3, list.Elections.Count);
            var ongoing = list.Elections.Single(e => e.Id == election.Id);
            Assert.Equal(ElectionStatus.Ongoing, ongoing.Status);
            Assert.True(ongoing.HasVoted);
            Assert.Equal(2, ongoing.CandidateCount);
            Assert.Equal(ElectionStatus.Upcoming, list.Elections.Single(e => e.Opis == "Przyszłe").Status);
            Assert.Equal(ElectionStatus.Ended, list.Elections.Single(e => e.Opis == "Minione").Status);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}

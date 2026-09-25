using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Tests.Services
{
    public sealed class AdminServiceTests : IDisposable
    {
        private readonly SqliteDatabase _db = new();

        private static AdminService CreateService(InternetVotingContext context) => new(context, TestData.Logger<AdminService>());

        [Fact]
        public async Task Add_election_validates_dates_and_uniqueness()
        {
            using var context = _db.CreateContext();
            var service = CreateService(context);
            var model = new ElectionFormViewModel { Opis = " Wybory 2026 ", DataRozpoczecia = TestData.Now, DataZakonczenia = TestData.Now.AddDays(1) };

            Assert.Equal(AddElectionStatus.Success, await service.AddElectionAsync(model));
            Assert.Equal("Wybory 2026", context.DataWyborows.Single().Opis);
            Assert.Equal(AddElectionStatus.Duplicate, await service.AddElectionAsync(model));
            Assert.Equal(AddElectionStatus.InvalidDates, await service.AddElectionAsync(new ElectionFormViewModel { Opis = "X", DataRozpoczecia = TestData.Now, DataZakonczenia = TestData.Now }));
        }

        [Fact]
        public async Task Add_candidate_requires_existing_election_and_unique_name_per_election()
        {
            using var context = _db.CreateContext();
            var first = TestData.OngoingElection("A");
            var second = TestData.OngoingElection("B");
            context.AddRange(first, second);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            Assert.Equal(AddCandidateStatus.Success, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = first.Id }));
            Assert.Equal(AddCandidateStatus.Duplicate, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = " Jan ", Nazwisko = "Nowak", IdWybory = first.Id }));
            Assert.Equal(AddCandidateStatus.Success, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = second.Id }));
            Assert.Equal(AddCandidateStatus.ElectionNotFound, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = 999 }));
            Assert.Equal(AddCandidateStatus.ElectionNotFound, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = null }));
        }

        [Fact]
        public async Task Delete_candidate_refuses_when_votes_exist()
        {
            using var context = _db.CreateContext();
            var election = TestData.OngoingElection();
            var voted = new Kandydat { Imie = "A", Nazwisko = "A", IdWyboryNavigation = election };
            var fresh = new Kandydat { Imie = "B", Nazwisko = "B", IdWyboryNavigation = election };
            context.AddRange(election, voted, fresh);
            context.GlosowanieWyborczes.Add(new GlosowanieWyborcze { IdKandydatNavigation = voted, IdWyboryNavigation = election, Indeks = 0, Nonce = new string('0', 32), Hash = new string('0', 64), ZnacznikCzasu = TestData.Now });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            Assert.Equal(DeleteCandidateStatus.HasVotes, await service.DeleteCandidateAsync(voted.Id));
            Assert.Equal(DeleteCandidateStatus.Success, await service.DeleteCandidateAsync(fresh.Id));
            Assert.Equal(DeleteCandidateStatus.NotFound, await service.DeleteCandidateAsync(fresh.Id));

            var list = await service.GetCandidatesAsync(election.Id);
            var item = Assert.Single(list.Candidates);
            Assert.Equal(voted.Id, item.Id);
            Assert.Equal(1, item.VoteCount);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}

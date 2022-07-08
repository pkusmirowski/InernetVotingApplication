using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InternetVotingApplicationTests
{
    [TestFixture]
    public class AdminServiceTests
    {
        private Kandydat? candidate;
        private DataWyborow? election;
        private DbContextOptions<InternetVotingContext>? options;

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<InternetVotingContext>().UseInMemoryDatabase(databaseName: "tempDB").Options;
        }

        [Test]
        public async Task AddCandidateAndElection_ReturnsTrueResults()
        {
            candidate = new Kandydat()
            {
                Id = 1,
                Imie = "Jan",
                Nazwisko = "Inny",
                IdWybory = 1
            };

            election = new DataWyborow()
            {
                Id = 1,
                DataRozpoczecia = new DateTime(2020, 1, 1),
                DataZakonczenia = new DateTime(2030, 1, 1),
                Opis = "Presidential election"
            };

            var context = new InternetVotingContext(options);
            var electionContext = new ElectionService(context);
            var repository = new AdminService(context, electionContext);

            bool electionAddResult = await repository.AddElectionAsync(election);
            bool candidateAddResult = await repository.AddCandidateAsync(candidate);

            Assert.IsTrue(electionAddResult);
            Assert.IsTrue(candidateAddResult);
        }
    }
}

using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InternetVotingApplicationTests
{
    [TestFixture]
    public class ElectionServiceTests
    {
        private DbContextOptions<InternetVotingContext>? options;

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<InternetVotingContext>().UseInMemoryDatabase(databaseName: "temp_DB").Options;
        }

        [Test]
        public void GetAllElectionTest_CheckElectionsIsNotNull()
        {
            var context = new InternetVotingContext(options);
            var electionService = new ElectionService(context);
            var test = electionService.GetAllElections();
            Assert.IsNotNull(test);
        }

        [Test]
        public void AddVoteTest()
        {
        }
    }
}

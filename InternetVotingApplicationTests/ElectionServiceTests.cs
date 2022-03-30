using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public void GetAllElectionTest()
        {
            var context = new InternetVotingContext(options);
            var repository = new ElectionService(context);
            var test = repository.GetAllElections();
            Assert.IsNotNull(test);
        }

        [Test]
        public void AddVoteTest()
        {
        }
    }
}

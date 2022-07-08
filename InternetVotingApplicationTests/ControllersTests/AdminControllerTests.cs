using InernetVotingApplication.Controllers;
using InernetVotingApplication.IServices;
using InernetVotingApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace InternetVotingApplicationTests.ControllersTests
{
    [TestFixture]
    public class AdminControllerTests
    {
        private Mock<IElectionService> _electionService = null!;
        private Mock<IAdminService> _adminService = null!;
        private AdminController _adminController = null!;

        [SetUp]
        public void SetUp()
        {
            _electionService = new Mock<IElectionService>();
            _adminService = new Mock<IAdminService>();
            _adminController = new AdminController(_electionService.Object, _adminService.Object);
        }

        [Test]
        public void AddCandidate_ReturnViewResult()
        {
            SessionManager.NotEmptySession(_adminController);
            _electionService.Setup(x => x.ShowElectionByName()).Returns(new List<DataWyborow>());
            var result = _adminController.AddCandidate();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task AddCandidate_ModelStateInvalid_ReturnView()
        {
            Kandydat candidate = CreateCandidate();
            _electionService.Setup(x => x.ShowElectionByName()).Returns(new List<DataWyborow>());
            _adminController.ModelState.AddModelError("test", "test");
            var result = await _adminController.AddCandidateAsync(candidate);

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task AddCandidate_ModelStateValid_ReturnSuccessView()
        {
            Kandydat candidate = CreateCandidate();
            _adminService.Setup(x => x.AddCandidateAsync(candidate)).ReturnsAsync(true);
            _electionService.Setup(x => x.ShowElectionByName()).Returns(new List<DataWyborow>());
            string viewBagResult = "Kandydat " + candidate.Imie + " " + candidate.Nazwisko + " został dodany do głosowania wyborczego!";
            var result = await _adminController.AddCandidateAsync(candidate) as ViewResult;

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(viewBagResult, result.ViewData["addCandidateSuccessful"]);
        }

        [Test]
        public async Task AddCandidate_ModelStateValid_ReturnFailureView()
        {
            Kandydat candidate = CreateCandidate();
            _adminService.Setup(x => x.AddCandidateAsync(candidate)).ReturnsAsync(false);
            _electionService.Setup(x => x.ShowElectionByName()).Returns(new List<DataWyborow>());
            var result = await _adminController.AddCandidateAsync(candidate) as ViewResult;

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(false, result.ViewData["Error"]);
        }

        [Test]
        public async Task CreateElection_ModelStateValid_SuccessfullAdded()
        {
            DataWyborow electionDate = CreateElectionDate();
            SessionManager.NotEmptySession(_adminController);
            _adminService.Setup(x => x.AddElectionAsync(electionDate)).ReturnsAsync(true);
            var result = await _adminController.CreateElectionAsync(electionDate) as ViewResult;
            const string viewBagResult = "Wybory zostały dodane!";

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(viewBagResult, result.ViewData["addElectionSuccessful"]);
        }

        [Test]
        public async Task CreateElection_ModelStateValid_UnsuccessfulAdded()
        {
            DataWyborow electionDate = CreateElectionDate();
            SessionManager.NotEmptySession(_adminController);
            _adminService.Setup(x => x.AddElectionAsync(electionDate)).ReturnsAsync(false);
            var result = await _adminController.CreateElectionAsync(electionDate) as ViewResult;

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(false, result.ViewData["Error"]);
        }

        private static Kandydat CreateCandidate()
        {
            return new Kandydat
            {
                Id = 1,
                Imie = "John",
                Nazwisko = "Inny",
                IdWybory = 1
            };
        }

        private static DataWyborow CreateElectionDate()
        {
            return new DataWyborow
            {
                Id = 1,
                DataRozpoczecia = new DateTime(2022, 4, 1),
                DataZakonczenia = new DateTime(2022, 7, 1),
                Opis = "Presidental election"
            };
        }
    }
}

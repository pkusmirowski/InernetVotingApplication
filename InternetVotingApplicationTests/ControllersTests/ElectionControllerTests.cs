using InernetVotingApplication.Controllers;
using InernetVotingApplication.IServices;
using InernetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace InternetVotingApplicationTests.ControllersTests
{
    [TestFixture]
    public class ElectionControllerTests
    {
        public Mock<IElectionService> _electionService = null!;
        public ElectionController _electionController = null!;

        [SetUp]
        public void SetUp()
        {
            _electionService = new Mock<IElectionService>();
            _electionController = new ElectionController(_electionService.Object);
        }

        [Test]
        public void ElectionDashboard_UserNotLogged_RetrunRedirectToLogin()
        {
            SessionManager.EmptySession(_electionController);
            var result = _electionController.Dashboard() as RedirectToActionResult;

            Assert.AreEqual("Login", result.ActionName);
        }

        [Test]
        public void ElectionDashboard_UserLogged_RetrunView()
        {
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.GetAllElections()).Returns(new DataWyborowViewModel());
            var result = _electionController.Dashboard() as ViewResult;

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task ElectionVoting_ElectionEnded_ReturnRedirectToAction()
        {
            const int id = 1;
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckIfElectionEnded(id)).Returns(true);
            _electionService.Setup(x => x.CheckElectionBlockchain(id)).Returns(true);
            var result = await _electionController.VotingAsync(id) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("ElectionResult", result.ActionName);
        }

        [Test]
        public async Task ElectionVoting_ElectionStarted_ReturnRedirectToAction()
        {
            const int id = 1;
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckIfElectionStarted(id)).Returns(true);
            _electionService.Setup(x => x.CheckElectionBlockchain(id)).Returns(true);
            var result = await _electionController.VotingAsync(id) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("ElectionResult", result.ActionName);
        }

        [Test]
        public async Task ElectionVoting_ElectionVoted_ReturnRedirectToAction()
        {
            const int id = 1;
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckIfVoted(It.IsAny<string>(), id)).ReturnsAsync(true);
            _electionService.Setup(x => x.CheckElectionBlockchain(id)).Returns(true);
            var result = await _electionController.VotingAsync(id) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("ElectionResult", result.ActionName);
        }

        [Test]
        public async Task ElectionVoting_ShowEelectionCandidates_ReturnView()
        {
            const int id = 1;
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckIfElectionEnded(id)).Returns(false);
            _electionService.Setup(x => x.CheckIfElectionStarted(id)).Returns(false);
            _electionService.Setup(x => x.CheckIfVoted(It.IsAny<string>(), id)).ReturnsAsync(false);
            _electionService.Setup(x => x.CheckElectionBlockchain(id)).Returns(true);
            var result = await _electionController.VotingAsync(id);

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task ElectionVoting_ElectionError_ReturnRedirectToElectionError()
        {
            const int id = 1;
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckElectionBlockchain(id)).Returns(false);
            var result = await _electionController.VotingAsync(id) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("ElectionError", result.ActionName);
        }

        [Test]
        public async Task AddVote_UserAlreadyVoted_ReturnRedirectToDashborad()
        {
            int[] candidate = { 1 };
            int[] election = { 1 };
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckIfVoted(It.IsAny<string>(), 1)).ReturnsAsync(true);
            var result = await _electionController.VotingAddAsync(candidate, election) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("ElectionResult", result.ActionName);
        }

        [Test]
        public async Task AddVote_SuccessfulVote_ReturnRedirectToVoted()
        {
            int[] candidate = { 1 };
            int[] election = { 1 };
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckIfVoted(It.IsAny<string>(), 1)).ReturnsAsync(false);
            _electionService.Setup(x => x.AddVote(It.IsAny<string>(), candidate[0], election[0])).Returns("teststring");
            var result = await _electionController.VotingAddAsync(candidate, election) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("Voted", result.ActionName);
        }

        [Test]
        public async Task AddVote_UnsuccessfulVote_ReturnRedirectToElectionError()
        {
            int[] candidate = { 1 };
            int[] election = { 1 };
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckIfVoted(It.IsAny<string>(), 1)).ReturnsAsync(false);
            _electionService.Setup(x => x.AddVote(It.IsAny<string>(), candidate[0], election[0])).Returns("");
            var result = await _electionController.VotingAddAsync(candidate, election) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("ElectionError", result.ActionName);
        }

        [Test]
        public void ElectionResult_ReturnElectionResults()
        {
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckElectionBlockchain(It.IsAny<int>())).Returns(true);
            var result = _electionController.ElectionResult(1, 1);

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public void ElectionResult_ReturnElectionResultsError()
        {
            SessionManager.NotEmptySession(_electionController);
            _electionService.Setup(x => x.CheckElectionBlockchain(It.IsAny<int>())).Returns(false);
            var result = _electionController.ElectionResult(1, 1) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("ElectionError", result.ActionName);
        }
    }
}

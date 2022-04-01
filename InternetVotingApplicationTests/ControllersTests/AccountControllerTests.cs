using InernetVotingApplication.Controllers;
using InernetVotingApplication.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace InternetVotingApplicationTests.ExtensionMethodsTests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private Mock<IUserService> _userService;
        private Mock<IElectionService> _electionService;
        private Mock<IAdminService> _adminService;
        private AccountController _accountController;

        [SetUp]
        public void SetUp()
        {
            _userService = new Mock<IUserService>();
            _electionService = new Mock<IElectionService>();
            _adminService = new Mock<IAdminService>();
            _accountController = new AccountController(_userService.Object, _electionService.Object, _adminService.Object);
        }

        [Test]
        public void CheckIfUserIsRegistered_ReturnView()
        {
            Mock<ISession> sessionMock = new();
            _accountController.ControllerContext.HttpContext = new DefaultHttpContext
            {
                Session = sessionMock.Object
            };

            var result = _accountController.Register();
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public void CheckIfUserIsRegistered_ReturnRedirectToDashboard()
        {
            var mockContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();
            const string sessionValue = "email";
            byte[] dummy = System.Text.Encoding.UTF8.GetBytes(sessionValue);
            mockSession.Setup(x => x.TryGetValue(It.IsAny<string>(), out dummy)).Returns(true);
            mockContext.Setup(s => s.Session).Returns(mockSession.Object);

            _accountController.ControllerContext.HttpContext = mockContext.Object;

            var result = _accountController.Register();
            Assert.IsInstanceOf<RedirectToActionResult>(result);
        }
    }
}

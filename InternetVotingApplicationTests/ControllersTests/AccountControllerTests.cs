using InernetVotingApplication.Controllers;
using InernetVotingApplication.IServices;
using InernetVotingApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;

namespace InternetVotingApplicationTests.ExtensionMethodsTests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private Mock<IUserService> _userService = null!;
        private Mock<IElectionService> _electionService = null!;
        private Mock<IAdminService> _adminService = null!;
        private AccountController _accountController = null!;

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
            SessionManager.EmptySession(_accountController);
            var result = _accountController.Register();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public void CheckIfUserIsRegistered_ReturnRedirectToDashboard()
        {
            SessionManager.NotEmptySession(_accountController);
            var result = _accountController.Register();

            Assert.IsInstanceOf<RedirectToActionResult>(result);
        }

        [Test]
        public async Task RegisterUser_UserAlreadyRegistered_ReturnRedirectToDashboard()
        {
            SessionManager.NotEmptySession(_accountController);
            Uzytkownik user = CreateNewUser();
            var result = await _accountController.RegisterAsync(user) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("Dashboard", result.ActionName);
        }

        [Test]
        public async Task RegisterUser_ModelStateInvalid_ReturnView()
        {
            Uzytkownik user = CreateNewUser();
            SessionManager.EmptySession(_accountController);
            _accountController.ModelState.AddModelError("error", "error");
            var result = await _accountController.RegisterAsync(user);

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task RegisterUser_RegisterNotSuccessful_ReturnView()
        {
            Uzytkownik user = CreateNewUser();
            SessionManager.EmptySession(_accountController);
            _userService.Setup(x => x.RegisterAsync(It.IsAny<Uzytkownik>())).ReturnsAsync(false);
            const bool viewBagResult = false;
            var result = await _accountController.RegisterAsync(user) as ViewResult;

            Assert.AreEqual(viewBagResult, result.ViewData["Error"]);
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]

        public async Task RegisterUser_RegisterSuccessful_ReturnView()
        {
            Uzytkownik user = CreateNewUser();
            SessionManager.EmptySession(_accountController);
            _userService.Setup(x => x.RegisterAsync(It.IsAny<Uzytkownik>())).ReturnsAsync(true);
            string viewBagResult = "Uzytkownik " + user.Imie + " " + user.Nazwisko + " został zarejestrowany poprawnie! </br> Aktywuj swoje konto potwierdzając adres E-mail";
            var result = await _accountController.RegisterAsync(user) as ViewResult;

            Assert.AreEqual(viewBagResult, result.ViewData["registrationSuccessful"]);
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task LoginUser_ModelStateInvalid_ReturnView()
        {
            Logowanie user = CreateLoginUser();
            SessionManager.EmptySession(_accountController);
            _accountController.ModelState.AddModelError("test", "test");
            var result = await _accountController.LoginAsync(user);

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task LoginAdmin_ModelStateValid_ReturnRedirectToAdminPanel()
        {
            Logowanie user = CreateLoginUser();
            SessionManager.EmptySession(_accountController);
            _userService.Setup(x => x.GetLoggedEmail(It.IsAny<Logowanie>())).Returns("test@op.pl");
            var result = await _accountController.LoginAsync(user) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("Panel", result.ActionName);
            Assert.AreEqual("Admin", result.ControllerName);
        }

        [Test]
        public async Task LoginUser_ModelStateValid_ReturnRedirectToUserPanel()
        {
            Logowanie user = CreateLoginUser();
            SessionManager.EmptySession(_accountController);
            _userService.Setup(x => x.LoginAsync(It.IsAny<Logowanie>())).ReturnsAsync(1);
            _userService.Setup(x => x.GetLoggedEmail(It.IsAny<Logowanie>())).Returns("test@op.pl");
            var result = await _accountController.LoginAsync(user) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("Dashboard", result.ActionName);
            Assert.AreEqual("Election", result.ControllerName);
        }

        [Test]
        public async Task LoginUser_ModelStateValid_ReturnError()
        {
            Logowanie user = CreateLoginUser();
            SessionManager.EmptySession(_accountController);
            _userService.Setup(x => x.LoginAsync(It.IsAny<Logowanie>())).ReturnsAsync(3);
            _userService.Setup(x => x.GetLoggedEmail(It.IsAny<Logowanie>())).Returns("test@op.pl");
            const bool viewResult = false;
            var result = await _accountController.LoginAsync(user) as ViewResult;

            Assert.AreEqual(viewResult, result.ViewData["Error"]);
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public void Logout_ReturnRedirectToAction()
        {
            SessionManager.EmptySession(_accountController);
            var result = _accountController.Logout() as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("Login", result.ActionName);
        }

        [Test]
        public void ChangePassword_EmptySession_ReturnView()
        {
            SessionManager.EmptySession(_accountController);
            var result = _accountController.ChangePassword() as ViewResult;

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public void ChangePassword_NotEmptySession_ReturnRedirectToAction()
        {
            SessionManager.NotEmptySession(_accountController);
            var result = _accountController.ChangePassword() as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("Login", result.ActionName);
        }

        [Test]
        public void ChangePassword_UserNotLogged_ReturnRedirectToLogin()
        {
            var userPasswords = new ChangePassword
            {
                Password = "P@ssw0rd",
                NewPassword = "P@@SWORD",
                ConfirmNewPassword = "P@@SWORD"
            };

            SessionManager.EmptySession(_accountController);
            var result = _accountController.ChangePassword(userPasswords) as RedirectToActionResult;

            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual("Login", result.ActionName);
        }

        [Test]
        public void SuccessfulChangedPassword_UserLogged_ModelStateIsValid_ReturnView()
        {
            var userPasswords = new ChangePassword
            {
                Password = "P@ssw0rd",
                NewPassword = "P@@SWORD",
                ConfirmNewPassword = "P@@SWORD"
            };
            SessionManager.NotEmptySession(_accountController);
            _userService.Setup(x => x.ChangePassword(userPasswords, It.IsAny<string>())).Returns(true);
            var result = _accountController.ChangePassword(userPasswords) as ViewResult;
            const string successfulViewBag = "Hasło zostało zmienione poprawnie!";

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(successfulViewBag, result.ViewData["changePasswordSuccessful"]);
        }

        [Test]
        public void UnsuccessfulChangedPassword_UserLogged_ModelStateIsValid_ReturnError()
        {
            var userPasswords = new ChangePassword
            {
                Password = "P@ssw0rd",
                NewPassword = "P@@SWORD",
                ConfirmNewPassword = "P@@SWORD"
            };
            SessionManager.NotEmptySession(_accountController);
            _userService.Setup(x => x.ChangePassword(userPasswords, It.IsAny<string>())).Returns(false);
            var result = _accountController.ChangePassword(userPasswords) as ViewResult;
            const bool error = false;

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(error, result.ViewData["Error"]);
        }

        [Test]
        public void ActivationAccount_AccountNotActivated_ReturnError()
        {
            var routeData = new RouteData();
            routeData.Values.Add("key1", "value1");
            _accountController.ControllerContext.RouteData = routeData;
            var result = _accountController.Activation() as ViewResult;
            const string error = "Zły kod aktywacyjny.";

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(error, result.ViewData["Message"]);
        }

        //To fix

        //[Test]
        //public void ActivationAccount_AccountSuccessfullyActivated_ReturnSuccessMessage()
        //{
        //    string error = "Aktywacja konta powiodła się.";
        //    var routeData = new RouteData();
        //    routeData.Values.Add("id", "id");
        //    _accountController.ControllerContext.RouteData = routeData;
        //    _userService.Setup(x => x.GetUserByAcitvationCode(new Guid())).Returns(true);
        //    var result = _accountController.Activation() as ViewResult;

        //    Assert.IsInstanceOf<ViewResult>(result);
        //    Assert.AreEqual(error, result.ViewData["Message"]);
        //}

        [Test]
        public async Task PasswordRecoveryTest_ModelStateInvalid_ReturnView()
        {
            PasswordRecovery password = GeneratePassword();
            _accountController.ModelState.AddModelError("test", "test");

            var result = await _accountController.PasswordRecoveryAsync(password);
            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task PasswordRecoveryTest_ModelStateIsValid_ReturnSuccess()
        {
            PasswordRecovery password = GeneratePassword();
            _userService.Setup(x => x.RecoverPassword(password)).ReturnsAsync(true);
            var result = await _accountController.PasswordRecoveryAsync(password) as ViewResult;

            const bool viewResult = true;
            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(viewResult, result.ViewData["Success"]);
        }
        

        [Test]
        public async Task PasswordRecoveryTest_ModelStateIsValid_ReturnFailure()
        {
            PasswordRecovery password = GeneratePassword();
            _userService.Setup(x => x.RecoverPassword(password)).ReturnsAsync(false);
            var result = await _accountController.PasswordRecoveryAsync(password) as ViewResult;

            const bool viewResult = false;
            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(viewResult, result.ViewData["Error"]);
        }

        [Test]
        public void SearchTest()
        {
            var result = _accountController.Search("test");
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ViewResult>(result);
        }
      
        private static Uzytkownik CreateNewUser()
        {
            return new Uzytkownik
            {
                Imie = "Jan",
                Nazwisko = "Inny"
            };
        }

        private static Logowanie CreateLoginUser()
        {
            return new Logowanie
            {
                Email = "test@op.pl",
                Haslo = "P@ssw0rd"
            };
        }

        private static PasswordRecovery GeneratePassword()
        {
            return new PasswordRecovery
            {
                Email = "test@op.pl",
                Pesel = "81032549648"
            };
        }
    }
}

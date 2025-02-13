using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InternetVotingApplication.Controllers
{
    public class AccountController(IUserService userService, IElectionService electionService, IAdminService adminService) : Controller
    {
        private readonly IUserService _userService = userService;
        private readonly IElectionService _electionService = electionService;
        private readonly IAdminService _adminService = adminService;

        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAsync(Uzytkownik user)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                return RedirectToAction("Dashboard");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            if (await _userService.RegisterAsync(user))
            {
                ViewBag.RegistrationSuccessful = $"Uzytkownik {user.Imie} {user.Nazwisko} został zarejestrowany poprawnie! </br> Aktywuj swoje konto potwierdzając adres E-mail";
                return View();
            }

            ViewBag.Error = "Registration failed. Please try again.";
            return View();
        }

        public async Task<IActionResult> LoginAsync(Logowanie user)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                return RedirectToAction("Dashboard");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var loginResult = await _userService.LoginAsync(user);
            if (loginResult == 0 || loginResult == 1)
            {
                HttpContext.Session.SetString("email", user.Email);
                if (loginResult == 0)
                {
                    HttpContext.Session.SetString("Admin", "Admin");
                    return RedirectToAction("Panel", "Admin");
                }
                return RedirectToAction("Dashboard", "Election");
            }

            ViewBag.Error = "Login failed. Please check your credentials.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("email") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePassword user)
        {
            if (HttpContext.Session.GetString("email") == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            if (_userService.ChangePassword(user, HttpContext.Session.GetString("email")))
            {
                ViewBag.ChangePasswordSuccessful = "Hasło zostało zmienione poprawnie!";
                return View();
            }

            ViewBag.Error = "Password change failed. Please try again.";
            return View();
        }

        public IActionResult Activation()
        {
            ViewBag.Message = "Zły kod aktywacyjny.";
            if (RouteData.Values["id"] != null && _userService.GetUserByAcitvationCode(new Guid(RouteData.Values["id"].ToString())))
            {
                ViewBag.Message = "Aktywacja konta powiodła się.";
            }

            return View();
        }

        public IActionResult PasswordRecovery()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PasswordRecoveryAsync(PasswordRecovery password)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            if (await _userService.RecoverPassword(password))
            {
                ViewBag.Success = "Password recovery successful. Please check your email.";
                return View();
            }

            ViewBag.Error = "Password recovery failed. Please try again.";
            return View();
        }

        public IActionResult Search(string text)
        {
            text ??= "1";
            var vm = _electionService.SearchVote(text);
            return View(vm);
        }
    }
}


using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InernetVotingApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;
        private readonly ElectionService _electionService;
        private readonly AdminService _adminService;
        public AccountController(UserService userService, ElectionService electionService, AdminService adminService)
        {
            _userService = userService;
            _electionService = electionService;
            _adminService = adminService;
        }
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

            if (ModelState.IsValid)
            {
                if (await _userService.Register(user))
                {
                    ViewBag.registrationSuccessful = "Uzytkownik " + user.Imie + " " + user.Nazwisko + " został zarejestrowany poprawnie! </br> Aktywuj swoje konto potwierdzając adres E-mail";
                    return View();
                }
                ViewBag.Error = false;
                return View();
            }
            return View();
        }

        public async Task<IActionResult> LoginAsync(Logowanie user)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                return RedirectToAction("Dashboard");
            }

            if (ModelState.IsValid)
            {
                var val = await _userService.LoginAsync(user);
                if (val == 0 || val == 1)
                {
                    if (val == 0)
                    {
                        //Zapisanie admina w sesji
                        string email = _userService.GetLoggedEmail(user);
                        HttpContext.Session.SetString("email", email);

                        const string admin = "Admin";
                        HttpContext.Session.SetString("Admin", admin);
                        return RedirectToAction("Panel", "Admin");
                    }
                    else
                    {
                        //Zapisanie użytkownika w sesji
                        string email = _userService.GetLoggedEmail(user);
                        HttpContext.Session.SetString("email", email);
                        return RedirectToAction("Dashboard", "Election");
                    }
                }
                ViewBag.Error = false;
                return View();
            }

            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult ChangePassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePassword user)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            var userEmail = HttpContext.Session.GetString("email");
            if (ModelState.IsValid)
            {
                if (_userService.ChangePassword(user, userEmail))
                {
                    ViewBag.changePasswordSuccessful = "Hasło zostało zmienione poprawnie!";
                    return View();
                }
                ViewBag.Error = false;
                return View();
            }
            return View();
        }

        public ActionResult Activation()
        {
            ViewBag.Message = "Zły kod aktywacyjny.";
            if (RouteData.Values["id"] != null)
            {
                Guid activationCode = new(RouteData.Values["id"].ToString());
                if (_userService.GetUserByAcitvationCode(activationCode))
                {
                    ViewBag.Message = "Aktywacja konta powiodła się.";
                }
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
            if (ModelState.IsValid)
            {
                if (await _userService.RecoverPassword(password))
                {
                    ViewBag.Success = true;
                    return View();
                }
                else
                {
                    ViewBag.Error = false;
                }
            }
            return View();
        }

        public IActionResult Search(string Text)
        {
            if (string.IsNullOrEmpty(Text))
            {
                Text = "1";
            }
            var vm = _electionService.SearchVote(Text);

            return View(vm);
        }
    }
}

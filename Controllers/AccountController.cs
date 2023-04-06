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
            if (ModelState.IsValid)
            {
                if (await _userService.RegisterAsync(user))
                {
                    ViewBag.registrationSuccessful = "Użytkownik " + user.Imie + " " + user.Nazwisko + " został zarejestrowany poprawnie! </br> Aktywuj swoje konto potwierdzając adres E-mail";
                    return View();
                }
                else
                {
                    return RedirectToAction("RegistrationError");
                }
            }
            else
            {
                return View(user);
            }
        }

        public IActionResult RegistrationError()
        {
            ViewBag.Error = "Błąd podczas rejestracji użytkownika.";
            return View("Register");
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

            var val = await _userService.LoginAsync(user);

            switch (val)
            {
                case 0:
                    // Zapisanie admina w sesji
                    HttpContext.Session.SetString("email", user.Email);
                    HttpContext.Session.SetString("Admin", "Admin");
                    return RedirectToAction("Panel", "Admin");

                case 1:
                    // Zapisanie użytkownika w sesji
                    HttpContext.Session.SetString("email", user.Email);
                    return RedirectToAction("Dashboard", "Election");

                default:
                    ViewBag.Error = false;
                    return View();
            }
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePassword user)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                if (await _userService.ChangePasswordAsync(user, HttpContext.Session.GetString("email")))
                {
                    ViewBag.changePasswordSuccessful = "Hasło zostało zmienione poprawnie!";
                    return View();
                }
                ViewBag.Error = false;
                return View();
            }
            return View();
        }

        // Obsługuje aktywację konta za pomocą kodu aktywacyjnego
        public async Task<ActionResult> Activation()
        {
            // Sprawdza, czy kod aktywacyjny został podany
            if (!RouteData.Values.TryGetValue("id", out object idValue))
            {
                ViewBag.Message = "Nie podano kodu aktywacyjnego.";
                return View();
            }

            // Sprawdza, czy kod aktywacyjny jest poprawny
            if (!Guid.TryParse(idValue.ToString(), out Guid activationCode))
            {
                ViewBag.Message = "Nieprawidłowy kod aktywacyjny.";
                return View();
            }

            // Wywołuje metodę serwisu, która zmienia stan konta i kod aktywacyjny
            if (await _userService.GetUserByActivationCodeAsync(activationCode))
            {
                ViewBag.Message = "Aktywacja konta powiodła się.";
            }
            else
            {
                ViewBag.Message = "Nieprawidłowy kod aktywacyjny.";
            }

            // Zwraca widok z odpowiednim komunikatem
            return View();
        }

        public IActionResult PasswordRecovery()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PasswordRecoveryAsync(PasswordRecovery passwordRecovery)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            if (await _userService.RecoverPassword(passwordRecovery))
            {
                ViewBag.Success = true;
                return View();
            }

            ViewBag.Error = false;
            return View();
        }

        public IActionResult Search(string Text = "1")
        {
            var vm = _electionService.SearchVote(Text);

            return View(vm);
        }
    }
}

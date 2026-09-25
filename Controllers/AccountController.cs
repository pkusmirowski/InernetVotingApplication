using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternetVotingApplication.Controllers
{
    public class AccountController(IUserService userService, IElectionService electionService, ILogger<AccountController> logger) : Controller
    {
        [HttpGet]
        public IActionResult Register()
        {
            return User.Identity?.IsAuthenticated == true
                ? RedirectToAction("Dashboard", "Election")
                : View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "Election");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var status = await userService.RegisterAsync(model, code => Url.Action("Activation", "Account", new { id = code }, Request.Scheme)!);
            switch (status)
            {
                case RegistrationStatus.Success:
                    return View("RegisterConfirmation", model);
                case RegistrationStatus.EmailTaken:
                    ModelState.AddModelError(nameof(model.Email), "Konto z tym adresem e-mail już istnieje.");
                    break;
                case RegistrationStatus.PeselTaken:
                    ModelState.AddModelError(nameof(model.Pesel), "Konto z tym numerem PESEL już istnieje.");
                    break;
                case RegistrationStatus.InvalidPesel:
                    ModelState.AddModelError(nameof(model.Pesel), "Numer PESEL jest niepoprawny.");
                    break;
                case RegistrationStatus.InvalidEmail:
                    ModelState.AddModelError(nameof(model.Email), "Adres e-mail jest niepoprawny.");
                    break;
                default:
                    ModelState.AddModelError(string.Empty, "Rejestracja nie powiodła się.");
                    break;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectAfterLogin(User.IsInRole(Roles.Admin), returnUrl);
            }

            return View(new Logowanie { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(Logowanie model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var outcome = await userService.LoginAsync(model);
            switch (outcome.Status)
            {
                case LoginStatus.Success:
                    await SignInAsync(outcome.User!, outcome.IsAdmin);
                    logger.LogInformation("User {UserId} signed in", outcome.User!.Id);
                    return RedirectAfterLogin(outcome.IsAdmin, model.ReturnUrl);
                case LoginStatus.NotActivated:
                    ModelState.AddModelError(string.Empty, "Konto nie zostało jeszcze aktywowane. Sprawdź swoją skrzynkę e-mail.");
                    break;
                case LoginStatus.LockedOut:
                    ModelState.AddModelError(string.Empty, "Konto zostało tymczasowo zablokowane po zbyt wielu nieudanych próbach logowania. Spróbuj ponownie później.");
                    break;
                default:
                    ModelState.AddModelError(string.Empty, "Nieprawidłowy adres e-mail lub hasło.");
                    break;
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePassword());
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePassword model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await userService.ChangePasswordAsync(User.GetUserId(), model))
            {
                TempData["StatusMessage"] = "Hasło zostało zmienione.";
                return RedirectToAction(nameof(ChangePassword));
            }

            ModelState.AddModelError(nameof(model.Password), "Obecne hasło jest nieprawidłowe.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Activation(Guid? id)
        {
            var activated = id.HasValue && await userService.ActivateAsync(id.Value);
            return View(activated);
        }

        [HttpGet]
        public IActionResult PasswordRecovery()
        {
            return View(new PasswordRecovery());
        }

        [HttpPost]
        public async Task<IActionResult> PasswordRecovery(PasswordRecovery model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await userService.RequestPasswordResetAsync(model.Email, token => Url.Action("ResetPassword", "Account", new { token }, Request.Scheme)!);
            return View("PasswordRecoveryConfirmation");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(Guid? token)
        {
            if (!token.HasValue || !await userService.IsPasswordResetTokenValidAsync(token.Value))
            {
                return View("ResetPasswordInvalid");
            }

            return View(new ResetPasswordViewModel { Token = token.Value });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await userService.ResetPasswordAsync(model.Token, model.NewPassword))
            {
                return View("ResetPasswordInvalid");
            }

            TempData["StatusMessage"] = "Hasło zostało ustawione. Możesz się zalogować.";
            return RedirectToAction(nameof(Login));
        }

        /// <summary>Public lookup of a vote by its hash (verifiability without signing in).</summary>
        [HttpGet]
        public async Task<IActionResult> Search(string? hash)
        {
            var vm = string.IsNullOrWhiteSpace(hash)
                ? new VoteSearchViewModel()
                : await electionService.SearchVoteAsync(hash);
            return View(vm);
        }

        private Task SignInAsync(Uzytkownik user, bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, $"{user.Imie} {user.Nazwisko}"),
                new(ClaimTypes.Role, isAdmin ? Roles.Admin : Roles.Voter),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });
        }

        private IActionResult RedirectAfterLogin(bool isAdmin, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return isAdmin
                ? RedirectToAction("Panel", "Admin")
                : RedirectToAction("Dashboard", "Election");
        }
    }
}

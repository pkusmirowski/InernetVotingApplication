using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InernetVotingApplication.Controllers
{
    public class AccountController : Controller
    {
        private UserService _userService;

        public AccountController(UserService userService)
        {
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("Username") != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(Uzytkownik user)
        {
            if (HttpContext.Session.GetString("Username") != null)
            {
                return RedirectToAction("Dashboard");
            }

            if (ModelState.IsValid)
            {
                if (_userService.Register(user))
                {
                    ViewBag.registrationSuccessful = "Uzytkownik " + user.Imie + " " + user.Nazwisko + " został zarejestrowany poprawnie!";
                    return View();
                }
                ViewBag.Error = false;
                return View();
            }
            return View();
        }

        public IActionResult Login(Logowanie user)
        {
            if (HttpContext.Session.GetString("Username") != null)
            {
                return RedirectToAction("Dashboard");
            }

            if (ModelState.IsValid)
            {
                string userName = "";
                if (_userService.Login(user, ref userName))
                {
                    //Zapisanie użytkownika w sesji

                    HttpContext.Session.SetString("Username", userName);
                    string test = "test";
                    HttpContext.Session.SetString("Role", test);
                    return RedirectToAction("Dashboard");
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

        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Login");
            }

            var vm = _userService.GetAll();
            //ViewBag.Username = HttpContext.Session.GetString("Username");
            //ViewBag.Role = HttpContext.Session.GetString("Role");
            return View(vm);
        }

        public IActionResult Voting()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }
    }
}
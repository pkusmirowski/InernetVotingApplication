using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Array = InernetVotingApplication.ExtensionMethods.ArrayExtensions;

namespace InernetVotingApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;

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
            if (HttpContext.Session.GetString("IdNumber") != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAsync(Uzytkownik user)
        {
            if (HttpContext.Session.GetString("IdNumber") != null)
            {
                return RedirectToAction("Dashboard");
            }

            if (ModelState.IsValid)
            {
                if (await _userService.Register(user))
                {
                    ViewBag.registrationSuccessful = "Uzytkownik " + user.Imie + " " + user.Nazwisko + " został zarejestrowany poprawnie!";
                    return View();
                }
                ViewBag.Error = false;
                return View();
            }
            return View();
        }

        public async Task<IActionResult> LoginAsync(Logowanie user)
        {
            if (HttpContext.Session.GetString("IdNumber") != null)
            {
                return RedirectToAction("Dashboard");
            }

            if (ModelState.IsValid)
            {
                if (await _userService.LoginAsync(user))
                {
                    //Zapisanie użytkownika w sesji
                    string IdNumber = "";
                    IdNumber = await _userService.GetLoggedIdNumber(user, IdNumber);
                    HttpContext.Session.SetString("IdNumber", IdNumber);
                    //string test = "test";
                    //HttpContext.Session.SetString("Role", test);
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("IdNumber")))
            {
                return RedirectToAction("Login");
            }

            var vm = _userService.GetAllElections();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> VotingAsync(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("IdNumber")))
            {
                return RedirectToAction("Login");
            }

            if (_userService.CheckElectionBlockchain(id) != false)
            {

                if (_userService.CheckIfElectionEnded(id) == false)
                {
                    return RedirectToAction("ElectionResult", new { @ver = 3, @result = id });
                }

                if (_userService.CheckIfElectionStarted(id) == false)
                {
                    return RedirectToAction("ElectionResult", new { @ver = 1, @result = id });
                }

                if (await _userService.CheckIfVoted(HttpContext.Session.GetString("IdNumber"), id))
                {
                    return RedirectToAction("ElectionResult", new { @ver = 2, @result = id });
                }

                @ViewBag.ID = id;

                var vm = _userService.GetAllCandidates(id);
                return View(vm);
            }
            return RedirectToAction("ElectionError");
        }

        [HttpPost]
        public async Task<IActionResult> VotingAddAsync(int[] candidate, int[] election)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("IdNumber")))
            {
                return RedirectToAction("Login");
            }

            if (Array.IsNullOrEmpty(candidate) || Array.IsNullOrEmpty(election))
            {
                return RedirectToAction("Dashboard");
            }

            int candidateId = candidate[0];
            int electionId = election[0];

            if (await _userService.CheckIfVoted(HttpContext.Session.GetString("IdNumber"), electionId))
            {
                return RedirectToAction("ElectionResult");
            }

            string ifAdded = _userService.AddVote(HttpContext.Session.GetString("IdNumber"), candidateId, electionId);
            string userHash = ifAdded;
            if (ifAdded.Length > 3)
            {
                ifAdded = "True";
            }
            else
            {
                ifAdded = "False";
            }

            if (Convert.ToBoolean(ifAdded))
            {
                return RedirectToAction("Voted", new { hash = userHash });
            }
            else
            {
                return RedirectToAction("ElectionError");
            }
        }

        public IActionResult ElectionError()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("IdNumber")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public IActionResult Voted(string hash)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("IdNumber")))
            {
                return RedirectToAction("Login");
            }

            @ViewBag.ID = hash;
            return View();
        }

        public IActionResult ElectionResult(int ver, int result)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("IdNumber")))
            {
                return RedirectToAction("Login");
            }

            if (_userService.CheckElectionBlockchain(result) != false)
            {
                @ViewBag.ID = ver;

                if (ver == 3)
                {
                    var vm = _userService.GetElectionResult(result);
                    return View(vm);
                }

                return View();
            }
            return RedirectToAction("ElectionError");
        }

        public IActionResult Search(string Text)
        {
            if (string.IsNullOrEmpty(Text))
            {
                Text = "1";
            }
            var vm = _userService.SearchVote(Text);

            return View(vm);
        }
    }
}
using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Array = InernetVotingApplication.ExtensionMethods.ArrayExtensions;

namespace InernetVotingApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserService _userService;
        private static readonly object obj = new();

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
                if (await _userService.Register(user).ConfigureAwait(false))
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
            if (HttpContext.Session.GetString("email") != null)
            {
                return RedirectToAction("Dashboard");
            }

            if (ModelState.IsValid)
            {
                var val = await _userService.LoginAsync(user).ConfigureAwait(false);
                if (val == 0 || val == 1)
                {
                    if (val == 0)
                    {
                        //Zapisanie admina w sesji
                        string email = await _userService.GetLoggedEmail(user).ConfigureAwait(false);
                        HttpContext.Session.SetString("email", email);

                        const string admin = "Admin";
                        HttpContext.Session.SetString("Admin", admin);
                        return RedirectToAction("Panel");
                    }
                    else
                    {
                        //Zapisanie użytkownika w sesji
                        string email = await _userService.GetLoggedEmail(user).ConfigureAwait(false);
                        HttpContext.Session.SetString("email", email);
                        return RedirectToAction("Dashboard");
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

        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            var vm = _userService.GetAllElections();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> VotingAsync(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Panel");
            }

            if (_userService.CheckElectionBlockchain(id))
            {
                if (!_userService.CheckIfElectionEnded(id))
                {
                    return RedirectToAction("ElectionResult", new { @ver = 3, @result = id });
                }

                if (!_userService.CheckIfElectionStarted(id))
                {
                    return RedirectToAction("ElectionResult", new { @ver = 1, @result = id });
                }

                if (await _userService.CheckIfVoted(HttpContext.Session.GetString("email"), id).ConfigureAwait(false))
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            if (Array.IsNullOrEmpty(candidate) || Array.IsNullOrEmpty(election))
            {
                return RedirectToAction("Dashboard");
            }

            int candidateId = candidate[0];
            int electionId = election[0];

            if (await _userService.CheckIfVoted(HttpContext.Session.GetString("email"), electionId).ConfigureAwait(false))
            {
                return RedirectToAction("ElectionResult");
            }

            string ifAdded;

            lock (obj)
            {
                ifAdded = _userService.AddVote(HttpContext.Session.GetString("email"), candidateId, electionId);
            }

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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public IActionResult Voted(string hash)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            @ViewBag.ID = hash;
            return View();
        }

        public IActionResult ElectionResult(int ver, int result)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            if (_userService.CheckElectionBlockchain(result))
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

        public IActionResult Panel()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login");
            }

            return View();
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
                if (_userService.ChagePassword(user, userEmail))
                {
                    ViewBag.changePasswordSuccessful = "Hasło zostało zmienione poprawnie!";
                    return View();
                }
                ViewBag.Error = false;
                return View();
            }
            return View();
        }

        public IActionResult AddCandidate()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login");
            }

            List<DataWyborow> electionIdList = new();
            electionIdList = _userService.ShowElectionByName();

            ViewBag.IdWybory = (List<SelectListItem>)electionIdList.ConvertAll(a =>
            {
                return new SelectListItem()
                {
                    Text = a.Opis,
                    Value = a.Id.ToString(),
                    Selected = false
                };
            });

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddCandidateAsync(Kandydat kandydat)
        {
            if (ModelState.IsValid)
            {
                if (await _userService.AddCandidate(kandydat).ConfigureAwait(false))
                {
                    ViewBag.addCandidateSuccessful = "Kandydat " + kandydat.Imie + " " + kandydat.Nazwisko + " został dodany do głosowania wyborczego!";
                }
                else
                {
                    ViewBag.Error = false;
                }
            }

            List<DataWyborow> electionIdList = new();
            electionIdList = _userService.ShowElectionByName();

            ViewBag.IdWybory = (List<SelectListItem>)electionIdList.ConvertAll(a =>
            {
                return new SelectListItem()
                {
                    Text = a.Opis,
                    Value = a.Id.ToString(),
                    Selected = false
                };
            });

            return View();
        }

        public IActionResult CreateElection(DataWyborow dataWyborow)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }
    }
}
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
        private readonly ElectionService _electionService;
        private readonly AdminService _adminService;
        private static readonly object obj = new();

        public AccountController(UserService userService, ElectionService electionService, AdminService adminService)
        {
            _userService = userService;
            _electionService = electionService;
            _adminService = adminService;
        }

        //public IActionResult Index()
        // {
        //  return View();
        // }

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
                        return RedirectToAction("Panel");
                    }
                    else
                    {
                        //Zapisanie użytkownika w sesji
                        string email = _userService.GetLoggedEmail(user);
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

        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            var vm = _electionService.GetAllElections();
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

            if (_electionService.CheckElectionBlockchain(id))
            {
                if (_electionService.CheckIfElectionEnded(id))
                {
                    return RedirectToAction("ElectionResult", new { @ver = 3, @result = id });
                }

                if (_electionService.CheckIfElectionStarted(id))
                {
                    return RedirectToAction("ElectionResult", new { @ver = 1, @result = id });
                }

                if (await _electionService.CheckIfVoted(HttpContext.Session.GetString("email"), id))
                {
                    return RedirectToAction("ElectionResult", new { @ver = 2, @result = id });
                }

                @ViewBag.ID = id;

                var vm = _electionService.GetAllCandidates(id);
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

            if (await _electionService.CheckIfVoted(HttpContext.Session.GetString("email"), electionId))
            {
                return RedirectToAction("ElectionResult");
            }

            string ifAdded = "";

            lock (obj)
            {
                ifAdded = _electionService.AddVote(HttpContext.Session.GetString("email"), candidateId, electionId);
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

            if (_electionService.CheckElectionBlockchain(result))
            {
                @ViewBag.ID = ver;

                if (ver == 3)
                {
                    var vm = _electionService.GetElectionResult(result);
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
            var vm = _electionService.SearchVote(Text);

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

        public IActionResult AddCandidate()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login");
            }

            List<DataWyborow> electionIdList = new();
            electionIdList = _electionService.ShowElectionByName();

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
                if (await _adminService.AddCandidate(kandydat))
                {
                    ViewBag.addCandidateSuccessful = "Kandydat " + kandydat.Imie + " " + kandydat.Nazwisko + " został dodany do głosowania wyborczego!";
                }
                else
                {
                    ViewBag.Error = false;
                }
            }

            List<DataWyborow> electionIdList = new();
            electionIdList = _electionService.ShowElectionByName();

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

        public IActionResult CreateElection()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateElectionAsync(DataWyborow dataWyborow)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                if (await _adminService.AddElectionAsync(dataWyborow))
                {
                    ViewBag.addElectionSuccessful = "Wybory zostały dodane!";
                }
                else
                {
                    ViewBag.Error = false;
                }
            }

            return View();
        }

    }
}
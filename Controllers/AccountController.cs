using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Array = InernetVotingApplication.ExtensionMethods.Extensions;

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
        public async Task<IActionResult> Register(Uzytkownik user)
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

        public async Task<IActionResult> Login(Logowanie user)
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("IdNumber")))
            {
                return RedirectToAction("Login");
            }

            var vm = _userService.GetAllElections();
            //ViewBag.IdNumber = HttpContext.Session.GetString("IdNumber");
            //ViewBag.Role = HttpContext.Session.GetString("Role");
            return View(vm);
        }

        [HttpGet]
        public IActionResult Voting(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("IdNumber")))
            {
                return RedirectToAction("Login");
            }

            if (_userService.CheckIfVoted(HttpContext.Session.GetString("IdNumber"), id))
            {
                return RedirectToAction("Dashboard");
            }

            @ViewBag.ID = id;
            var vm = _userService.GetAllCandidates(id);
            return View(vm);
        }

        [HttpPost]
        public IActionResult VotingAdd(int[] candidate, int[] election)
        {

            if (Array.IsNullOrEmpty(candidate) || Array.IsNullOrEmpty(election))
            {
                return RedirectToAction("Dashboard");
            }

            int candidateId = candidate[0];
            int electionId = election[0];

            if (_userService.CheckIfVoted(HttpContext.Session.GetString("IdNumber"), electionId))
            {
                return RedirectToAction("Dashboard");
            }

            if (_userService.AddVote(HttpContext.Session.GetString("IdNumber"), candidateId, electionId))
            {
                return RedirectToAction("Dashboard");
            }
            //ModelState.Remove("hiddenValue");
            ModelState.Clear();
            return RedirectToAction("Voting");
        }
    }
}
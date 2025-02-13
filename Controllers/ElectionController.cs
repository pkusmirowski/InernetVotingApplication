using InternetVotingApplication.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Array = InternetVotingApplication.ExtensionMethods.ArrayExtensions;

namespace InternetVotingApplication.Controllers
{
    public class ElectionController : Controller
    {
        private readonly IElectionService _electionService;
        private static readonly object _lock = new();

        public ElectionController(IElectionService electionService)
        {
            _electionService = electionService;
        }

        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login", "Account");
            }

            var vm = _electionService.GetAllElections();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> VotingAsync(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Panel", "Admin");
            }

            if (!_electionService.CheckElectionBlockchain(id))
            {
                return RedirectToAction("ElectionError");
            }

            if (!_electionService.CheckIfElectionEnded(id))
            {
                return RedirectToAction("ElectionResult", new { ver = 3, result = id });
            }

            if (!_electionService.CheckIfElectionStarted(id))
            {
                return RedirectToAction("ElectionResult", new { ver = 1, result = id });
            }

            if (await _electionService.CheckIfVoted(HttpContext.Session.GetString("email"), id))
            {
                return RedirectToAction("ElectionResult", new { ver = 2, result = id });
            }

            ViewBag.ID = id;
            var vm = _electionService.GetAllCandidates(id);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> VotingAddAsync(int[] candidate, int[] election)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login", "Account");
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

            string ifAdded;
            lock (_lock)
            {
                ifAdded = _electionService.AddVote(HttpContext.Session.GetString("email"), candidateId, electionId);
            }

            if (ifAdded.Length > 3)
            {
                return RedirectToAction("Voted", new { hash = ifAdded });
            }

            return RedirectToAction("ElectionError");
        }

        public IActionResult ElectionError()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        public IActionResult Voted(string hash)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.ID = hash;
            return View();
        }

        public IActionResult ElectionResult(int ver, int result)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!_electionService.CheckElectionBlockchain(result))
            {
                return RedirectToAction("ElectionError");
            }

            ViewBag.ID = ver;

            if (ver == 3)
            {
                var vm = _electionService.GetElectionResult(result);
                return View(vm);
            }

            return View();
        }
    }
}
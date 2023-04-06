using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Array = InernetVotingApplication.ExtensionMethods.ArrayExtensions;

namespace InernetVotingApplication.Controllers
{
    public class ElectionController : Controller
    {
        private readonly ElectionService _electionService;
        private static readonly object obj = new();
        public ElectionController(ElectionService electionService)
        {
            _electionService = electionService;
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

            if (!_electionService.CheckElectionBlockchain(id))
            {
                return RedirectToAction("ElectionError");
            }

            if (_electionService.CheckIfElectionEnded(id))
            {
                return RedirectToAction("ElectionResult", new { @ver = 3, @result = id });
            }

            if (!_electionService.CheckIfElectionStarted(id))
            {
                return RedirectToAction("ElectionResult", new { @ver = 1, @result = id });
            }

            if (await _electionService.CheckIfVoted(HttpContext.Session.GetString("email"), id).ConfigureAwait(false))
            {
                return RedirectToAction("ElectionResult", new { @ver = 2, @result = id });
            }

            @ViewBag.ID = id;

            var vm = _electionService.GetAllCandidates(id);
            return View(vm);
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

            string userHash = await _electionService.AddVoteAsync(HttpContext.Session.GetString("email"), candidateId, electionId);

            if (userHash == "0")
            {
                return RedirectToAction("ElectionError");
            }
            else
            {
                return RedirectToAction("Voted", new { hash = userHash });
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

            ViewBag.ID = hash;
            return View();
        }

        public IActionResult ElectionResult(int ver, int result)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")))
            {
                return RedirectToAction("Login");
            }

            if (!_electionService.CheckElectionBlockchain(result))
            {
                return RedirectToAction("ElectionError");
            }

            if (ver == 3)
            {
                var vm = _electionService.GetElectionResult(result);
                ViewBag.ID = "Wyniki wyborów";
                return View(vm);
            }
            else if (ver == 1)
            {
                ViewBag.ID = "Wybory jeszcze się nie zaczęły";
                return View("ElectionNotStarted");
            }
            else if (ver == 2)
            {
                ViewBag.ID = "Już oddałeś swój głos";
                return View("AlreadyVoted");
            }

            return View();
        }
    }
}

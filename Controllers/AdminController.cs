using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace InternetVotingApplication.Controllers
{
    public class AdminController : Controller
    {
        private readonly IElectionService _electionService;
        private readonly IAdminService _adminService;

        public AdminController(IElectionService electionService, IAdminService adminService)
        {
            _electionService = electionService;
            _adminService = adminService;
        }

        public IActionResult Panel()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")) || string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        public IActionResult AddCandidate()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login", "Account");
            }

            var electionIdList = _electionService.ShowElectionByName();
            ViewBag.IdWybory = electionIdList.ConvertAll(a => new SelectListItem
            {
                Text = a.Opis,
                Value = a.Id.ToString()
            });

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddCandidateAsync(Kandydat kandydat)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            if (await _adminService.AddCandidateAsync(kandydat))
            {
                ViewBag.AddCandidateSuccessful = $"Kandydat {kandydat.Imie} {kandydat.Nazwisko} został dodany do głosowania wyborczego!";
            }
            else
            {
                ViewBag.Error = "Adding candidate failed. Please try again.";
            }

            var electionIdList = _electionService.ShowElectionByName();
            ViewBag.IdWybory = electionIdList.ConvertAll(a => new SelectListItem
            {
                Text = a.Opis,
                Value = a.Id.ToString()
            });

            return View();
        }

        public IActionResult CreateElection()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateElectionAsync(DataWyborow dataWyborow)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            if (await _adminService.AddElectionAsync(dataWyborow))
            {
                ViewBag.AddElectionSuccessful = "Wybory zostały dodane!";
            }
            else
            {
                ViewBag.Error = "Adding election failed. Please try again.";
            }

            return View();
        }
    }
}


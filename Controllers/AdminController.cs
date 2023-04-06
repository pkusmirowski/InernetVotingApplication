using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace InernetVotingApplication.Controllers
{
    public class AdminController : Controller
    {
        private readonly ElectionService _electionService;
        private readonly AdminService _adminService;

        public AdminController(ElectionService electionService, AdminService adminService)
        {
            _electionService = electionService;
            _adminService = adminService;
        }

        public IActionResult Panel()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("email")) || string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public async Task<IActionResult> AddCandidate()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Admin")))
            {
                return RedirectToAction("Login");
            }

            ViewBag.IdWybory = (await _electionService.ShowElectionByNameAsync())
                                .Select(a => new SelectListItem { Text = a.Opis, Value = a.Id.ToString() });

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> AddCandidateAsync(Kandydat kandydat)
        {
            if (ModelState.IsValid)
            {
                if (await _adminService.AddCandidateAsync(kandydat))
                {
                    ViewBag.addCandidateSuccessful = $"Kandydat {kandydat.Imie} {kandydat.Nazwisko} został dodany do głosowania wyborczego!";
                }
                else
                {
                    ViewBag.Error = false;
                }
            }

            ViewBag.IdWybory = (await _electionService.ShowElectionByNameAsync()).Select(a => new SelectListItem { Text = a.Opis, Value = a.Id.ToString() });

            return View();
        }


        public IActionResult CreateElection()
        {
            var isAdmin = HttpContext.Session.GetString("Admin");

            if (string.IsNullOrEmpty(isAdmin) || isAdmin != "true")
                return RedirectToAction("Login");

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

using InernetVotingApplication.Models;
using InernetVotingApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
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

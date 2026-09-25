using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternetVotingApplication.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class AdminController(IAdminService adminService, IChainService chainService) : Controller
    {
        [HttpGet]
        public IActionResult Panel()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Elections()
        {
            return View(await chainService.GetAdminOverviewAsync());
        }

        [HttpPost]
        public async Task<IActionResult> VerifyChain(int id)
        {
            var result = await chainService.VerifyAndStoreAsync(id, "Manual", User.GetUserId());
            TempData["StatusMessage"] = result.IsValid
                ? $"Łańcuch wyborów {id} jest poprawny ({result.BlockCount} bloków)."
                : $"Łańcuch wyborów {id} NIE przechodzi weryfikacji. Niepoprawne bloki: {string.Join(", ", result.InvalidBlockIds.Concat(result.InvalidSignatureBlockIds).Distinct())}.";
            return RedirectToAction(nameof(Elections));
        }

        [HttpPost]
        public async Task<IActionResult> PublishAnchor(int id)
        {
            var anchor = await chainService.PublishAnchorAsync(id, ChainService.ReasonManual, User.GetUserId());
            TempData["StatusMessage"] = anchor == null
                ? "Wybory nie istnieją."
                : $"Opublikowano kotwicę: {anchor.BlockCount} bloków, głowa {anchor.HeadHash ?? "(pusta)"}.";
            return RedirectToAction(nameof(Elections));
        }

        [HttpGet]
        public async Task<IActionResult> Audit()
        {
            return View(await adminService.GetAuditLogAsync(200));
        }

        [HttpGet]
        public IActionResult CreateElection()
        {
            return View(new ElectionFormViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateElection(ElectionFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            switch (await adminService.AddElectionAsync(model, User.GetUserId()))
            {
                case AddElectionStatus.Success:
                    TempData["StatusMessage"] = $"Wybory \"{model.Opis}\" zostały utworzone.";
                    return RedirectToAction(nameof(CreateElection));
                case AddElectionStatus.Duplicate:
                    ModelState.AddModelError(nameof(model.Opis), "Wybory o tej nazwie już istnieją.");
                    break;
                default:
                    ModelState.AddModelError(nameof(model.DataZakonczenia), "Data zakończenia musi być późniejsza niż data rozpoczęcia.");
                    break;
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddCandidate(int? electionId)
        {
            await PopulateElectionsAsync(electionId);
            return View(new CandidateFormViewModel { IdWybory = electionId });
        }

        [HttpPost]
        public async Task<IActionResult> AddCandidate(CandidateFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateElectionsAsync(model.IdWybory);
                return View(model);
            }

            switch (await adminService.AddCandidateAsync(model, User.GetUserId()))
            {
                case AddCandidateStatus.Success:
                    TempData["StatusMessage"] = $"Kandydat {model.Imie} {model.Nazwisko} został dodany.";
                    return RedirectToAction(nameof(AddCandidate), new { electionId = model.IdWybory });
                case AddCandidateStatus.Duplicate:
                    ModelState.AddModelError(nameof(model.Nazwisko), "Ten kandydat już bierze udział w wybranych wyborach.");
                    break;
                default:
                    ModelState.AddModelError(nameof(model.IdWybory), "Wybrane wybory nie istnieją.");
                    break;
            }

            await PopulateElectionsAsync(model.IdWybory);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteCandidate(int? electionId)
        {
            var vm = await adminService.GetCandidatesAsync(electionId);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCandidate(int id, int? electionId)
        {
            TempData["StatusMessage"] = await adminService.DeleteCandidateAsync(id, User.GetUserId()) switch
            {
                DeleteCandidateStatus.Success => "Kandydat został usunięty.",
                DeleteCandidateStatus.HasVotes => "Nie można usunąć kandydata, na którego oddano już głosy.",
                _ => "Kandydat nie istnieje.",
            };

            return RedirectToAction(nameof(DeleteCandidate), new { electionId });
        }

        private async Task PopulateElectionsAsync(int? selected)
        {
            var options = await adminService.GetElectionOptionsAsync();
            ViewBag.Elections = new SelectList(options, nameof(ElectionOptionViewModel.Id), nameof(ElectionOptionViewModel.Opis), selected);
        }
    }
}

using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetVotingApplication.Controllers
{
    [Authorize]
    public class ElectionController(IElectionService electionService, IResultsService resultsService, IChainService chainService) : Controller
    {
        private const string ReceiptHashKey = "VoteReceiptHash";
        private const string ReceiptElectionKey = "VoteReceiptElection";

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var vm = await electionService.GetElectionListAsync(User.GetUserId());
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Voting(int id)
        {
            if (User.IsInRole(Roles.Admin))
            {
                return RedirectToAction("Panel", "Admin");
            }

            var status = await electionService.GetElectionStatusAsync(id);
            if (status == null)
            {
                return NotFound();
            }

            if (status != ElectionStatus.Ongoing || await electionService.HasVotedAsync(User.GetUserId(), id))
            {
                return RedirectToAction(nameof(ElectionResult), new { id });
            }

            var vm = await electionService.GetVotingPageAsync(id);
            return vm == null ? NotFound() : View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Vote(KandydatViewModel model)
        {
            if (User.IsInRole(Roles.Admin))
            {
                return RedirectToAction("Panel", "Admin");
            }

            if (!ModelState.IsValid || model.SelectedCandidateId is null)
            {
                return await RedisplayVotingPageAsync(model.ElectionId, "Wybierz kandydata, na którego chcesz zagłosować.");
            }

            var outcome = await electionService.CastVoteAsync(User.GetUserId(), model.ElectionId, model.SelectedCandidateId.Value);
            switch (outcome.Status)
            {
                case VoteStatus.Success:
                    TempData[ReceiptHashKey] = outcome.Hash;
                    TempData[ReceiptElectionKey] = outcome.ElectionName;
                    return RedirectToAction(nameof(Voted));
                case VoteStatus.ElectionNotFound:
                    return NotFound();
                case VoteStatus.CandidateNotInElection:
                    return await RedisplayVotingPageAsync(model.ElectionId, "Wybrany kandydat nie bierze udziału w tych wyborach.");
                case VoteStatus.Conflict:
                    return await RedisplayVotingPageAsync(model.ElectionId, "Serwer jest chwilowo zajęty. Twój głos nie został zapisany, spróbuj ponownie.");
                case VoteStatus.ChainCorrupted:
                    return View("ElectionError", outcome.ElectionName);
                case VoteStatus.AlreadyVoted:
                case VoteStatus.ElectionNotStarted:
                case VoteStatus.ElectionEnded:
                default:
                    return RedirectToAction(nameof(ElectionResult), new { id = model.ElectionId });
            }
        }

        [HttpGet]
        public IActionResult Voted()
        {
            if (TempData[ReceiptHashKey] is not string hash || TempData[ReceiptElectionKey] is not string election)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            return View(new VoteReceiptViewModel(hash, election));
        }

        [HttpGet]
        public async Task<IActionResult> ElectionResult(int id)
        {
            var vm = await resultsService.GetResultsAsync(id);
            if (vm == null)
            {
                return NotFound();
            }

            vm.HasVoted = await electionService.HasVotedAsync(User.GetUserId(), id);
            return View(vm);
        }

        /// <summary>Public page: chain head, verification log, anchors, public key and (after the end) results.</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Chain(int id)
        {
            var vm = await chainService.GetChainPageAsync(id);
            if (vm == null)
            {
                return NotFound();
            }

            if (vm.Status == ElectionStatus.Ended)
            {
                vm.Results = await resultsService.GetResultsAsync(id);
            }

            return View(vm);
        }

        /// <summary>Public JSON export for independent verification (see tools/ChainVerifier).</summary>
        [HttpGet]
        [AllowAnonymous]
        [Produces("application/json")]
        public async Task<IActionResult> Export(int id)
        {
            var export = await chainService.ExportAsync(id);
            if (export == null)
            {
                return NotFound();
            }

            Response.Headers.ContentDisposition = $"attachment; filename=\"election-{id}-chain.json\"";
            return Json(export);
        }

        private async Task<IActionResult> RedisplayVotingPageAsync(int electionId, string error)
        {
            var vm = await electionService.GetVotingPageAsync(electionId);
            if (vm == null)
            {
                return NotFound();
            }

            ModelState.Clear();
            ModelState.AddModelError(nameof(KandydatViewModel.SelectedCandidateId), error);
            return View("Voting", vm);
        }
    }
}

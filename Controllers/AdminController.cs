using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Security;
using InternetVotingApplication.Services;
using InternetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InternetVotingApplication.Controllers;

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
        var result = await chainService.VerifyAndStoreAsync(id, ChainService.TriggerManual, User.GetUserId());
        TempData.SetStatus(
            result.IsValid
                ? $"Kontrola rejestru głosów zakończona bez zastrzeżeń. Sprawdzono głosów: {result.BlockCount}."
                : $"Kontrola rejestru głosów wykryła nieprawidłowości. Numery błędnych wpisów: {string.Join(", ", result.InvalidBlockIds.Concat(result.InvalidSignatureBlockIds).Distinct())}.",
            isError: !result.IsValid);
        return RedirectToAction(nameof(Elections));
    }

    [HttpPost]
    public async Task<IActionResult> PublishAnchor(int id)
    {
        var anchor = await chainService.PublishAnchorAsync(id, ChainService.ReasonManual, User.GetUserId());
        TempData.SetStatus(
            anchor switch
            {
                null => "Wybory nie istnieją.",
                { Recipients: { Length: > 0 } } => $"Kopia kontrolna została zapisana i wysłana do komisji. Liczba głosów w rejestrze: {anchor.BlockCount}.",
                _ => $"Kopia kontrolna została zapisana. Nie wysłano jej e-mailem, bo nie skonfigurowano odbiorców (Chain:AnchorRecipients). Liczba głosów w rejestrze: {anchor.BlockCount}.",
            },
            isError: anchor == null);
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
                TempData.SetStatus($"Wybory \"{model.Opis}\" zostały utworzone.");
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
    public async Task<IActionResult> EditElection(int id)
    {
        var model = await adminService.GetElectionAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        ViewBag.ElectionId = id;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditElection(int id, ElectionFormViewModel model)
    {
        ViewBag.ElectionId = id;
        var current = await adminService.GetElectionAsync(id);
        if (current == null)
        {
            return NotFound();
        }

        model.Status = current.Status;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        switch (await adminService.UpdateElectionAsync(id, model, User.GetUserId()))
        {
            case UpdateElectionStatus.Success:
                TempData.SetStatus($"Wybory \"{model.Opis}\" zostały zapisane.");
                return RedirectToAction(nameof(Elections));
            case UpdateElectionStatus.NotFound:
                return NotFound();
            case UpdateElectionStatus.Duplicate:
                ModelState.AddModelError(nameof(model.Opis), "Wybory o tej nazwie już istnieją.");
                break;
            case UpdateElectionStatus.StartLocked:
                ModelState.AddModelError(nameof(model.DataRozpoczecia), "Głosowanie już trwa, więc daty rozpoczęcia nie można zmienić.");
                break;
            case UpdateElectionStatus.ElectionEnded:
                ModelState.AddModelError(string.Empty, "Głosowanie się zakończyło, więc dat nie można zmienić. Można poprawić tylko nazwę.");
                break;
            case UpdateElectionStatus.EndBeforeLastVote:
                ModelState.AddModelError(nameof(model.DataZakonczenia), "Data zakończenia nie może być wcześniejsza niż ostatni oddany głos ani niż rozpoczęcie.");
                break;
            case UpdateElectionStatus.Conflict:
                ModelState.AddModelError(string.Empty, "W tym czasie ktoś oddał głos. Sprawdź dane i zapisz jeszcze raz.");
                break;
            default:
                ModelState.AddModelError(nameof(model.DataZakonczenia), "Data zakończenia musi być późniejsza niż data rozpoczęcia.");
                break;
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteElection(int id)
    {
        var status = await adminService.DeleteElectionAsync(id, User.GetUserId());
        TempData.SetStatus(
            status switch
            {
                DeleteElectionStatus.Success => "Wybory zostały usunięte.",
                DeleteElectionStatus.HasVotes => "Nie można usunąć wyborów, w których oddano już głosy.",
                _ => "Wybory nie istnieją.",
            },
            isError: status != DeleteElectionStatus.Success);

        return RedirectToAction(nameof(Elections));
    }

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        return View(await adminService.GetUsersAsync());
    }

    [HttpPost]
    public async Task<IActionResult> ActivateUser(int id)
    {
        var status = await adminService.ActivateUserAsync(id, User.GetUserId());
        TempData.SetStatus(
            status switch
            {
                UserActionStatus.Success => "Konto zostało aktywowane.",
                UserActionStatus.NoChange => "To konto jest już aktywne.",
                _ => "Konto nie istnieje.",
            },
            isError: status is not (UserActionStatus.Success or UserActionStatus.NoChange));

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    public async Task<IActionResult> SetAdministrator(int id, bool isAdmin)
    {
        var status = await adminService.SetAdministratorAsync(id, isAdmin, User.GetUserId());
        TempData.SetStatus(
            status switch
            {
                UserActionStatus.Success => isAdmin ? "Konto dostało uprawnienia administratora." : "Konto nie ma już uprawnień administratora.",
                UserActionStatus.NoChange => "To konto ma już takie uprawnienia.",
                UserActionStatus.Forbidden => "Nie można odebrać uprawnień sobie ani ostatniemu administratorowi.",
                _ => "Konto nie istnieje.",
            },
            isError: status is not (UserActionStatus.Success or UserActionStatus.NoChange));

        return RedirectToAction(nameof(Users));
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
                TempData.SetStatus($"Kandydat {model.Imie} {model.Nazwisko} został dodany.");
                return RedirectToAction(nameof(AddCandidate), new { electionId = model.IdWybory });
            case AddCandidateStatus.Duplicate:
                ModelState.AddModelError(nameof(model.Nazwisko), "Ten kandydat już bierze udział w wybranych wyborach.");
                break;
            case AddCandidateStatus.ElectionStarted:
                ModelState.AddModelError(nameof(model.IdWybory), "Głosowanie w tych wyborach już się rozpoczęło, więc listy kandydatów nie można zmienić.");
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
        var status = await adminService.DeleteCandidateAsync(id, User.GetUserId());
        TempData.SetStatus(
            status switch
            {
                DeleteCandidateStatus.Success => "Kandydat został usunięty.",
                DeleteCandidateStatus.HasVotes => "Nie można usunąć kandydata, na którego oddano już głosy.",
                DeleteCandidateStatus.ElectionStarted => "Głosowanie w tych wyborach już się rozpoczęło, więc listy kandydatów nie można zmienić.",
                _ => "Kandydat nie istnieje.",
            },
            isError: status != DeleteCandidateStatus.Success);

        return RedirectToAction(nameof(DeleteCandidate), new { electionId });
    }

    private async Task PopulateElectionsAsync(int? selected)
    {
        var options = await adminService.GetElectionOptionsAsync();
        ViewBag.Elections = new SelectList(options, nameof(ElectionOptionViewModel.Id), nameof(ElectionOptionViewModel.Opis), selected);
    }
}

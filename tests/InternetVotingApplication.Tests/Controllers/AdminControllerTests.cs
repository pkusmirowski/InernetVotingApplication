using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Controllers;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NSubstitute;
using static InternetVotingApplication.Tests.Controllers.ControllerTestHelper;

namespace InternetVotingApplication.Tests.Controllers;

public class AdminControllerTests
{
    private readonly IAdminService _admin = Substitute.For<IAdminService>();
    private readonly IChainService _chain = Substitute.For<IChainService>();

    private AdminController Create() => new AdminController(_admin, _chain).Prepare(Voter(id: 9, admin: true));

    [Fact]
    public async Task Elections_shows_overview()
    {
        _chain.GetAdminOverviewAsync().Returns([new AdminElectionViewModel { Id = 1 }]);

        var view = AssertView(await Create().Elections());

        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AdminElectionViewModel>>(view.Model));
    }

    [Fact]
    public async Task VerifyChain_reports_result_and_attributes_it_to_the_admin()
    {
        _chain.VerifyAndStoreAsync(3, "Manual", 9).Returns(new ChainVerificationResult(false, 5, "H", [2], [4]));
        var controller = Create();

        AssertRedirect(await controller.VerifyChain(3), "Elections");

        var message = Assert.IsType<string>(controller.TempData["StatusMessage"]);
        Assert.Contains("wykryła nieprawidłowości", message, StringComparison.Ordinal);
        Assert.Contains("2, 4", message, StringComparison.Ordinal);
        Assert.Equal(true, controller.TempData["StatusIsError"]);
    }

    [Fact]
    public async Task PublishAnchor_handles_missing_election()
    {
        _chain.PublishAnchorAsync(3, ChainService.ReasonManual, 9).Returns((ChainAnchorViewModel?)null);
        var controller = Create();

        AssertRedirect(await controller.PublishAnchor(3), "Elections");

        Assert.Equal("Wybory nie istnieją.", controller.TempData["StatusMessage"]);
    }

    [Fact]
    public async Task CreateElection_maps_statuses()
    {
        var model = new ElectionFormViewModel { Opis = "W", DataRozpoczecia = DateTime.Now, DataZakonczenia = DateTime.Now.AddDays(1) };

        _admin.AddElectionAsync(model, 9).Returns(AddElectionStatus.Success);
        var controller = Create();
        AssertRedirect(await controller.CreateElection(model), "CreateElection");
        Assert.NotNull(controller.TempData["StatusMessage"]);

        _admin.AddElectionAsync(model, 9).Returns(AddElectionStatus.Duplicate);
        controller = Create();
        AssertView(await controller.CreateElection(model));
        Assert.NotEmpty(controller.ModelState["Opis"]!.Errors);

        _admin.AddElectionAsync(model, 9).Returns(AddElectionStatus.InvalidDates);
        controller = Create();
        AssertView(await controller.CreateElection(model));
        Assert.NotEmpty(controller.ModelState["DataZakonczenia"]!.Errors);
    }

    [Fact]
    public async Task AddCandidate_populates_election_list_and_maps_statuses()
    {
        _admin.GetElectionOptionsAsync().Returns([new ElectionOptionViewModel(1, "A"), new ElectionOptionViewModel(2, "B")]);
        var controller = Create();

        var getView = AssertView(await controller.AddCandidate(2));
        Assert.Equal(2, Assert.IsType<CandidateFormViewModel>(getView.Model).IdWybory);
        var list = Assert.IsType<SelectList>((object)controller.ViewBag.Elections);
        Assert.Equal(2, list.Count());

        var model = new CandidateFormViewModel { Imie = "A", Nazwisko = "B", IdWybory = 2 };
        _admin.AddCandidateAsync(model, 9).Returns(AddCandidateStatus.Duplicate);
        controller = Create();
        AssertView(await controller.AddCandidate(model));
        Assert.NotEmpty(controller.ModelState["Nazwisko"]!.Errors);

        _admin.AddCandidateAsync(model, 9).Returns(AddCandidateStatus.ElectionNotFound);
        controller = Create();
        AssertView(await controller.AddCandidate(model));
        Assert.NotEmpty(controller.ModelState["IdWybory"]!.Errors);

        _admin.AddCandidateAsync(model, 9).Returns(AddCandidateStatus.Success);
        var redirect = AssertRedirect(await Create().AddCandidate(model), "AddCandidate");
        Assert.Equal(2, redirect.RouteValues!["electionId"]);
    }

    [Theory]
    [InlineData(DeleteCandidateStatus.Success, "usunięty")]
    [InlineData(DeleteCandidateStatus.HasVotes, "oddano już głosy")]
    [InlineData(DeleteCandidateStatus.NotFound, "nie istnieje")]
    public async Task DeleteCandidate_post_reports_outcome(DeleteCandidateStatus status, string fragment)
    {
        _admin.DeleteCandidateAsync(4, 9).Returns(status);
        var controller = Create();

        var redirect = AssertRedirect(await controller.DeleteCandidate(4, 2), "DeleteCandidate");

        Assert.Equal(2, redirect.RouteValues!["electionId"]);
        Assert.Contains(fragment, Assert.IsType<string>(controller.TempData["StatusMessage"]), StringComparison.Ordinal);
        Assert.Equal(status != DeleteCandidateStatus.Success, controller.TempData.ContainsKey("StatusIsError"));
    }

    [Fact]
    public async Task EditElection_returns_404_for_unknown_id_and_maps_statuses()
    {
        _admin.GetElectionAsync(5).Returns((ElectionFormViewModel?)null);
        Assert.IsType<NotFoundResult>(await Create().EditElection(5));

        var model = new ElectionFormViewModel { Opis = "W", DataRozpoczecia = DateTime.Now, DataZakonczenia = DateTime.Now.AddDays(1) };
        Assert.IsType<NotFoundResult>(await Create().EditElection(5, model));

        _admin.GetElectionAsync(5).Returns(new ElectionFormViewModel { Opis = "W", Status = ElectionStatus.Ongoing });
        _admin.UpdateElectionAsync(5, model, 9).Returns(UpdateElectionStatus.Success);
        var controller = Create();
        AssertRedirect(await controller.EditElection(5, model), "Elections");
        Assert.NotNull(controller.TempData["StatusMessage"]);

        _admin.UpdateElectionAsync(5, model, 9).Returns(UpdateElectionStatus.Duplicate);
        controller = Create();
        AssertView(await controller.EditElection(5, model));
        Assert.NotEmpty(controller.ModelState["Opis"]!.Errors);

        _admin.UpdateElectionAsync(5, model, 9).Returns(UpdateElectionStatus.StartLocked);
        controller = Create();
        AssertView(await controller.EditElection(5, model));
        Assert.NotEmpty(controller.ModelState["DataRozpoczecia"]!.Errors);
        Assert.Equal(ElectionStatus.Ongoing, model.Status);

        _admin.UpdateElectionAsync(5, model, 9).Returns(UpdateElectionStatus.EndBeforeLastVote);
        controller = Create();
        AssertView(await controller.EditElection(5, model));
        Assert.NotEmpty(controller.ModelState["DataZakonczenia"]!.Errors);

        _admin.UpdateElectionAsync(5, model, 9).Returns(UpdateElectionStatus.ElectionEnded);
        controller = Create();
        AssertView(await controller.EditElection(5, model));
        Assert.NotEmpty(controller.ModelState[string.Empty]!.Errors);

        _admin.UpdateElectionAsync(5, model, 9).Returns(UpdateElectionStatus.NotFound);
        Assert.IsType<NotFoundResult>(await Create().EditElection(5, model));
    }

    [Theory]
    [InlineData(DeleteElectionStatus.Success, "usunięte")]
    [InlineData(DeleteElectionStatus.HasVotes, "oddano już głosy")]
    [InlineData(DeleteElectionStatus.NotFound, "nie istnieją")]
    public async Task DeleteElection_reports_outcome(DeleteElectionStatus status, string fragment)
    {
        _admin.DeleteElectionAsync(4, 9).Returns(status);
        var controller = Create();

        AssertRedirect(await controller.DeleteElection(4), "Elections");

        Assert.Contains(fragment, Assert.IsType<string>(controller.TempData["StatusMessage"]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Users_page_lists_accounts_and_actions_report_outcome()
    {
        _admin.GetUsersAsync().Returns([new UserListItemViewModel { Id = 1 }]);
        var view = AssertView(await Create().Users());
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<UserListItemViewModel>>(view.Model));

        _admin.ActivateUserAsync(3, 9).Returns(UserActionStatus.Success);
        var controller = Create();
        AssertRedirect(await controller.ActivateUser(3), "Users");
        Assert.Contains("aktywowane", Assert.IsType<string>(controller.TempData["StatusMessage"]), StringComparison.Ordinal);

        _admin.SetAdministratorAsync(9, false, 9).Returns(UserActionStatus.Forbidden);
        controller = Create();
        AssertRedirect(await controller.SetAdministrator(9, false), "Users");
        Assert.Contains("ostatniemu administratorowi", Assert.IsType<string>(controller.TempData["StatusMessage"]), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Audit_shows_recent_entries()
    {
        _admin.GetAuditLogAsync(200).Returns([new AuditEntryViewModel { Action = "X" }]);

        var view = AssertView(await Create().Audit());

        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AuditEntryViewModel>>(view.Model));
    }
}

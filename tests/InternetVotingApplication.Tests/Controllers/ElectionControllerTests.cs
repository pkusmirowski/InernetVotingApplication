using InternetVotingApplication.Controllers;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using static InternetVotingApplication.Tests.Controllers.ControllerTestHelper;

namespace InternetVotingApplication.Tests.Controllers;

public class ElectionControllerTests
{
    private readonly IElectionService _elections = Substitute.For<IElectionService>();
    private readonly IResultsService _results = Substitute.For<IResultsService>();
    private readonly IChainService _chain = Substitute.For<IChainService>();

    private ElectionController Create(bool admin = false, int userId = 1)
        => new ElectionController(_elections, _results, _chain).Prepare(Voter(userId, admin));

    [Fact]
    public async Task Dashboard_lists_elections_for_the_signed_in_user()
    {
        _elections.GetElectionListAsync(4).Returns(new DataWyborowViewModel());

        var view = AssertView(await Create(userId: 4).Dashboard());

        Assert.IsType<DataWyborowViewModel>(view.Model);
    }

    [Fact]
    public async Task Voting_redirects_admins_to_panel()
    {
        AssertRedirect(await Create(admin: true).Voting(1), "Panel", "Admin");
        AssertRedirect(await Create(admin: true).Vote(new KandydatViewModel()), "Panel", "Admin");
    }

    [Fact]
    public async Task Voting_returns_404_for_unknown_election()
    {
        _elections.GetElectionStatusAsync(9).Returns((ElectionStatus?)null);

        Assert.IsType<NotFoundResult>(await Create().Voting(9));
    }

    [Theory]
    [InlineData(ElectionStatus.Upcoming, false)]
    [InlineData(ElectionStatus.Ended, false)]
    [InlineData(ElectionStatus.Ongoing, true)]
    public async Task Voting_redirects_to_results_unless_ongoing_and_not_voted(ElectionStatus status, bool hasVoted)
    {
        _elections.GetElectionStatusAsync(2).Returns(status);
        _elections.HasVotedAsync(1, 2).Returns(hasVoted);

        var redirect = AssertRedirect(await Create().Voting(2), "ElectionResult");
        Assert.Equal(2, redirect.RouteValues!["id"]);
    }

    [Fact]
    public async Task Voting_shows_candidates_when_allowed()
    {
        _elections.GetElectionStatusAsync(2).Returns(ElectionStatus.Ongoing);
        _elections.HasVotedAsync(1, 2).Returns(false);
        _elections.GetVotingPageAsync(2).Returns(new KandydatViewModel { ElectionId = 2 });

        var view = AssertView(await Create().Voting(2));

        Assert.Equal(2, Assert.IsType<KandydatViewModel>(view.Model).ElectionId);
    }

    [Fact]
    public async Task Vote_without_selection_redisplays_page_with_error()
    {
        _elections.GetVotingPageAsync(2).Returns(new KandydatViewModel { ElectionId = 2 });
        var controller = Create();

        var view = AssertView(await controller.Vote(new KandydatViewModel { ElectionId = 2 }), "Voting");

        Assert.NotEmpty(controller.ModelState["SelectedCandidateId"]!.Errors);
        await _elections.DidNotReceiveWithAnyArgs().CastVoteAsync(default, default, default);
    }

    [Fact]
    public async Task Vote_success_stores_receipt_in_tempdata_and_redirects()
    {
        _elections.CastVoteAsync(1, 2, 5).Returns(new VoteOutcome(VoteStatus.Success, "HASH", "Wybory"));
        var controller = Create();

        AssertRedirect(await controller.Vote(new KandydatViewModel { ElectionId = 2, SelectedCandidateId = 5 }), "Voted");

        Assert.Equal("HASH", controller.TempData["VoteReceiptHash"]);
        Assert.Equal("Wybory", controller.TempData["VoteReceiptElection"]);
    }

    [Theory]
    [InlineData(VoteStatus.AlreadyVoted)]
    [InlineData(VoteStatus.ElectionEnded)]
    [InlineData(VoteStatus.ElectionNotStarted)]
    public async Task Vote_outside_rules_redirects_to_results(VoteStatus status)
    {
        _elections.CastVoteAsync(1, 2, 5).Returns(new VoteOutcome(status));

        AssertRedirect(await Create().Vote(new KandydatViewModel { ElectionId = 2, SelectedCandidateId = 5 }), "ElectionResult");
    }

    [Fact]
    public async Task Vote_maps_remaining_outcomes()
    {
        _elections.GetVotingPageAsync(2).Returns(new KandydatViewModel { ElectionId = 2 });

        _elections.CastVoteAsync(1, 2, 5).Returns(new VoteOutcome(VoteStatus.ElectionNotFound));
        Assert.IsType<NotFoundResult>(await Create().Vote(new KandydatViewModel { ElectionId = 2, SelectedCandidateId = 5 }));

        _elections.CastVoteAsync(1, 2, 5).Returns(new VoteOutcome(VoteStatus.CandidateNotInElection));
        AssertView(await Create().Vote(new KandydatViewModel { ElectionId = 2, SelectedCandidateId = 5 }), "Voting");

        _elections.CastVoteAsync(1, 2, 5).Returns(new VoteOutcome(VoteStatus.Conflict));
        AssertView(await Create().Vote(new KandydatViewModel { ElectionId = 2, SelectedCandidateId = 5 }), "Voting");

        _elections.CastVoteAsync(1, 2, 5).Returns(new VoteOutcome(VoteStatus.ChainCorrupted, ElectionName: "X"));
        var error = AssertView(await Create().Vote(new KandydatViewModel { ElectionId = 2, SelectedCandidateId = 5 }), "ElectionError");
        Assert.Equal("X", error.Model);
    }

    [Fact]
    public void Voted_without_receipt_goes_back_to_dashboard()
    {
        AssertRedirect(Create().Voted(), "Dashboard");
    }

    [Fact]
    public void Voted_shows_receipt_from_tempdata()
    {
        var controller = Create();
        controller.TempData["VoteReceiptHash"] = "HASH";
        controller.TempData["VoteReceiptElection"] = "Wybory";

        var view = AssertView(controller.Voted());

        var receipt = Assert.IsType<VoteReceiptViewModel>(view.Model);
        Assert.Equal("HASH", receipt.Hash);
    }

    [Fact]
    public async Task ElectionResult_returns_404_or_fills_has_voted()
    {
        _results.GetResultsAsync(9).Returns((GlosowanieWyborczeViewModel?)null);
        Assert.IsType<NotFoundResult>(await Create().ElectionResult(9));

        _results.GetResultsAsync(2).Returns(new GlosowanieWyborczeViewModel { ElectionId = 2 });
        _elections.HasVotedAsync(1, 2).Returns(true);
        var view = AssertView(await Create().ElectionResult(2));
        Assert.True(Assert.IsType<GlosowanieWyborczeViewModel>(view.Model).HasVoted);
    }

    [Fact]
    public async Task Chain_adds_results_only_for_ended_elections()
    {
        _chain.GetChainPageAsync(2).Returns(new ChainViewModel { ElectionId = 2, Status = ElectionStatus.Ongoing });
        var ongoing = AssertView(await Create().Chain(2));
        Assert.Null(Assert.IsType<ChainViewModel>(ongoing.Model).Results);

        _chain.GetChainPageAsync(3).Returns(new ChainViewModel { ElectionId = 3, Status = ElectionStatus.Ended });
        _results.GetResultsAsync(3).Returns(new GlosowanieWyborczeViewModel { ElectionId = 3 });
        var ended = AssertView(await Create().Chain(3));
        Assert.NotNull(Assert.IsType<ChainViewModel>(ended.Model).Results);

        _chain.GetChainPageAsync(9).Returns((ChainViewModel?)null);
        Assert.IsType<NotFoundResult>(await Create().Chain(9));
    }

    [Fact]
    public async Task Export_returns_json_attachment()
    {
        var export = new ChainExport("internet-voting-chain/v2", DateTime.Now, new ChainExportElection(2, "W", DateTime.Now, DateTime.Now, 0, null), "k", "pem", [], [], []);
        _chain.ExportAsync(2).Returns(export);
        _elections.GetElectionStatusAsync(2).Returns(ElectionStatus.Ended);
        var controller = Create();

        var json = Assert.IsType<JsonResult>(await controller.Export(2));

        Assert.Same(export, json.Value);
        Assert.Contains("election-2-chain.json", controller.Response.Headers.ContentDisposition.ToString(), StringComparison.Ordinal);

        _elections.GetElectionStatusAsync(9).Returns((ElectionStatus?)null);
        Assert.IsType<NotFoundResult>(await Create().Export(9));
    }

    [Fact]
    public async Task Export_of_a_running_election_is_hidden_from_everyone()
    {
        var export = new ChainExport("internet-voting-chain/v2", DateTime.Now, new ChainExportElection(3, "W", DateTime.Now, DateTime.Now, 0, null), "k", "pem", [], [], []);
        _chain.ExportAsync(3).Returns(export);
        _elections.GetElectionStatusAsync(3).Returns(ElectionStatus.Ongoing);

        Assert.IsType<NotFoundResult>(await Create().Export(3));
        Assert.IsType<NotFoundResult>(await Create(admin: true).Export(3));

        _elections.GetElectionStatusAsync(3).Returns(ElectionStatus.Upcoming);
        Assert.IsType<NotFoundResult>(await Create().Export(3));
    }
}

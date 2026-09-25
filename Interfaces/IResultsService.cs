using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces
{
    public interface IResultsService
    {
        Task<GlosowanieWyborczeViewModel?> GetResultsAsync(int electionId);

        Task<VoteSearchViewModel> SearchVoteAsync(string hash);
    }
}

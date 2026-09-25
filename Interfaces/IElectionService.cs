using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces
{
    public sealed record VoteOutcome(VoteStatus Status, string? Hash = null, string? ElectionName = null);

    public interface IElectionService
    {
        Task<DataWyborowViewModel> GetElectionListAsync(int userId);

        Task<ElectionStatus?> GetElectionStatusAsync(int electionId);

        Task<KandydatViewModel?> GetVotingPageAsync(int electionId);

        Task<bool> HasVotedAsync(int userId, int electionId);

        Task<VoteOutcome> CastVoteAsync(int userId, int electionId, int candidateId);

        Task<GlosowanieWyborczeViewModel?> GetResultsAsync(int electionId);

        Task<VoteSearchViewModel> SearchVoteAsync(string hash);

        Task<ChainVerificationResult> VerifyChainAsync(int electionId);
    }
}

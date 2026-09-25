using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces
{
    public sealed record VoteOutcome(VoteStatus Status, string? Hash = null, string? ElectionName = null);

    /// <summary>Voter-facing operations: listing elections and casting a vote.</summary>
    public interface IElectionService
    {
        Task<DataWyborowViewModel> GetElectionListAsync(int userId);

        Task<ElectionStatus?> GetElectionStatusAsync(int electionId);

        Task<KandydatViewModel?> GetVotingPageAsync(int electionId);

        Task<bool> HasVotedAsync(int userId, int electionId);

        Task<VoteOutcome> CastVoteAsync(int userId, int electionId, int candidateId);
    }
}

using InternetVotingApplication.Blockchain;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces
{
    public interface IChainService
    {
        /// <summary>Full O(n) verification of one chain; the result is stored in the verification log.</summary>
        Task<ChainVerificationResult> VerifyAndStoreAsync(int electionId, string trigger, int? actorUserId = null);

        /// <summary>Latest stored verification, or null when the chain was never verified.</summary>
        Task<ChainVerificationViewModel?> GetLastVerificationAsync(int electionId);

        /// <summary>Signs the current head and records it; e-mails it to the configured recipients.</summary>
        Task<ChainAnchorViewModel?> PublishAnchorAsync(int electionId, string reason, int? actorUserId = null);

        Task<ChainViewModel?> GetChainPageAsync(int electionId);

        Task<ChainExport?> ExportAsync(int electionId);

        Task<IReadOnlyList<AdminElectionViewModel>> GetAdminOverviewAsync();

        /// <summary>Elections that the background worker should keep verifying and anchoring.</summary>
        Task<IReadOnlyList<int>> GetActiveElectionIdsAsync();

        Task<bool> HasFinalAnchorAsync(int electionId);
    }
}

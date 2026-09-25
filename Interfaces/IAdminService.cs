using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces
{
    public interface IAdminService
    {
        Task<AddElectionStatus> AddElectionAsync(ElectionFormViewModel model, int? actorUserId = null);

        Task<AddCandidateStatus> AddCandidateAsync(CandidateFormViewModel model, int? actorUserId = null);

        Task<IReadOnlyList<ElectionOptionViewModel>> GetElectionOptionsAsync();

        Task<CandidateListViewModel> GetCandidatesAsync(int? electionId);

        Task<DeleteCandidateStatus> DeleteCandidateAsync(int candidateId, int? actorUserId = null);

        Task<IReadOnlyList<AuditEntryViewModel>> GetAuditLogAsync(int take);
    }
}

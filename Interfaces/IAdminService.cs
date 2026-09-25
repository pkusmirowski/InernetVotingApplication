using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces
{
    public interface IAdminService
    {
        Task<AddElectionStatus> AddElectionAsync(ElectionFormViewModel model);

        Task<AddCandidateStatus> AddCandidateAsync(CandidateFormViewModel model);

        Task<IReadOnlyList<ElectionOptionViewModel>> GetElectionOptionsAsync();

        Task<CandidateListViewModel> GetCandidatesAsync(int? electionId);

        Task<DeleteCandidateStatus> DeleteCandidateAsync(int candidateId);
    }
}

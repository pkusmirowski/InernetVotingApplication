using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces;

public interface IAdminService
{
    Task<AddElectionStatus> AddElectionAsync(ElectionFormViewModel model, int? actorUserId = null);

    Task<AddCandidateStatus> AddCandidateAsync(CandidateFormViewModel model, int? actorUserId = null);

    Task<IReadOnlyList<ElectionOptionViewModel>> GetElectionOptionsAsync();

    Task<CandidateListViewModel> GetCandidatesAsync(int? electionId);

    Task<DeleteCandidateStatus> DeleteCandidateAsync(int candidateId, int? actorUserId = null);

    Task<IReadOnlyList<AuditEntryViewModel>> GetAuditLogAsync(int take);

    /// <summary>Form model of an existing election, or null when it does not exist.</summary>
    Task<ElectionFormViewModel?> GetElectionAsync(int electionId);

    Task<UpdateElectionStatus> UpdateElectionAsync(int electionId, ElectionFormViewModel model, int? actorUserId = null);

    /// <summary>Removes an election together with its candidates, anchors and verification runs; refused once a vote was cast.</summary>
    Task<DeleteElectionStatus> DeleteElectionAsync(int electionId, int? actorUserId = null);

    Task<IReadOnlyList<UserListItemViewModel>> GetUsersAsync();

    Task<UserActionStatus> ActivateUserAsync(int userId, int? actorUserId = null);

    /// <summary>Grants or revokes the administrator role. The actor cannot revoke their own role and the last administrator stays.</summary>
    Task<UserActionStatus> SetAdministratorAsync(int userId, bool isAdmin, int? actorUserId = null);
}

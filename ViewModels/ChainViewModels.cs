using InternetVotingApplication.Models;

namespace InternetVotingApplication.ViewModels
{
    public class ChainVerificationViewModel
    {
        public DateTime Date { get; set; }

        public bool IsValid { get; set; }

        public int BlockCount { get; set; }

        public string? HeadHash { get; set; }

        public string Trigger { get; set; } = string.Empty;

        public string? Details { get; set; }
    }

    public class ChainAnchorViewModel
    {
        public DateTime Date { get; set; }

        public int BlockCount { get; set; }

        public string? HeadHash { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Recipients { get; set; }

        public string Signature { get; set; } = string.Empty;
    }

    /// <summary>Public chain page of one election.</summary>
    public class ChainViewModel
    {
        public int ElectionId { get; set; }

        public string ElectionName { get; set; } = string.Empty;

        public DateTime DataRozpoczecia { get; set; }

        public DateTime DataZakonczenia { get; set; }

        public ElectionStatus Status { get; set; }

        public int BlockCount { get; set; }

        public string? HeadHash { get; set; }

        public string KeyId { get; set; } = string.Empty;

        public string PublicKeyPem { get; set; } = string.Empty;

        public ChainVerificationViewModel? LastVerification { get; set; }

        public IReadOnlyList<ChainAnchorViewModel> Anchors { get; set; } = [];

        /// <summary>Filled only for ended elections.</summary>
        public GlosowanieWyborczeViewModel? Results { get; set; }
    }

    /// <summary>Row of the administrator's election overview.</summary>
    public class AdminElectionViewModel
    {
        public int Id { get; set; }

        public string Opis { get; set; } = string.Empty;

        public DateTime DataRozpoczecia { get; set; }

        public DateTime DataZakonczenia { get; set; }

        public ElectionStatus Status { get; set; }

        public int CandidateCount { get; set; }

        public int BlockCount { get; set; }

        public int Participants { get; set; }

        public string? HeadHash { get; set; }

        public ChainVerificationViewModel? LastVerification { get; set; }

        public int AnchorCount { get; set; }

        public bool HasFinalAnchor { get; set; }
    }

    public class AuditEntryViewModel
    {
        public DateTime Date { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? Details { get; set; }
    }
}

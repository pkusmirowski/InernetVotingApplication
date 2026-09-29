using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ViewModels;

public class VoteSearchViewModel
{
    [Display(Name = "Kod potwierdzenia głosu")]
    [StringLength(64, ErrorMessage = ValidationMessages.TooLong)]
    public string? Hash { get; set; }

    public bool Searched { get; set; }

    public bool Found { get; set; }

    public string? CandidateName { get; set; }

    public string? CandidateSurname { get; set; }

    public string? ElectionName { get; set; }

    public int BlockIndex { get; set; }

    public DateTime Timestamp { get; set; }

    public bool ChainValid { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? Signature { get; set; }

    public int ElectionId { get; set; }
}

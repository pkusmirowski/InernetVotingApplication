using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ViewModels
{
    /// <summary>Voting page: the candidates of one election and the voter's choice.</summary>
    public class KandydatViewModel
    {
        public int ElectionId { get; set; }

        public string ElectionName { get; set; } = string.Empty;

        public DateTime DataZakonczenia { get; set; }

        public IReadOnlyList<KandydatItemViewModel> Candidates { get; set; } = [];

        [Required(ErrorMessage = "Wybierz kandydata")]
        [Display(Name = "Kandydat")]
        public int? SelectedCandidateId { get; set; }
    }
}

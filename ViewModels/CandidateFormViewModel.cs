using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ViewModels
{
    public class CandidateFormViewModel
    {
        [Required(ErrorMessage = "Wpisz imię kandydata")]
        [StringLength(50)]
        [Display(Name = "Imię")]
        public string Imie { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wpisz nazwisko kandydata")]
        [StringLength(50)]
        [Display(Name = "Nazwisko")]
        public string Nazwisko { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wybierz wybory")]
        [Display(Name = "Wybory")]
        public int? IdWybory { get; set; }
    }
}

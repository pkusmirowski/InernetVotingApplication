using System.ComponentModel.DataAnnotations;

namespace InernetVotingApplication.Models
{
    public class Logowanie
    {
        [Required(ErrorMessage = "Wpisz swój numer dowodu osobistego")]
        [StringLength(9)]
        [Display(Name = "Nr dowodu osobistego")]
        public string NumerDowodu { get; set; }

        [Required(ErrorMessage = "Podaj hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Haslo { get; set; }
    }
}
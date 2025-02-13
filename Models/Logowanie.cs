using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.Models
{
    public class Logowanie
    {
        [Required(ErrorMessage = "Wpisz swój adres email")]
        [StringLength(89)]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres email")]
        [Display(Name = "Adres Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Podaj hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Haslo { get; set; }
    }
}
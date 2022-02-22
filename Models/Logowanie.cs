using System.ComponentModel.DataAnnotations;

namespace InernetVotingApplication.Models
{
    public class Logowanie
    {
        [Required(ErrorMessage = "Wpisz swój adres email")]
        [StringLength(89)]
        [Display(Name = "Adres Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Podaj hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Haslo { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.Models
{
    public class PasswordRecovery
    {
        [Required(ErrorMessage = "Wpisz swój adres email")]
        [StringLength(89)]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres email")]
        [Display(Name = "Adres Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Wpisz swój PESEL")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "PESEL musi składać się z 11 cyfr")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "PESEL może zawierać tylko cyfry")]
        [Display(Name = "PESEL")]
        public string Pesel { get; set; }
    }
}

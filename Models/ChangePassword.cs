using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.Models
{
    public class ChangePassword
    {
        [Required(ErrorMessage = "Podaj hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Obecne hasło")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Podaj nowe hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{8,}$", ErrorMessage = "Hasło musi zawierać małą i dużą literę, cyfrę, specjalny symbol i mieć co najmniej 8 znaków.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Powtórz hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        [Compare("NewPassword", ErrorMessage = "Potwierdzenie hasła nie zgadza się z nowym hasłem.")]
        public string ConfirmNewPassword { get; set; }
    }
}

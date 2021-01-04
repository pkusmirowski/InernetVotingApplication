using System.ComponentModel.DataAnnotations;

namespace InernetVotingApplication.Models
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
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{8,}$", ErrorMessage = "Hasło musi zawierać: małą i dużą litere, cyfre, specjalny symbol, 8 znaków")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Powtórz hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        public string ConfirmNewPassword { get; set; }
    }
}
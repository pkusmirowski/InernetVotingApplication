using InternetVotingApplication.Models;
using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public Guid Token { get; set; }

        [Required(ErrorMessage = "Podaj nowe hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.Message)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Powtórz nowe hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        [Compare(nameof(NewPassword), ErrorMessage = "Hasła się nie zgadzają")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}

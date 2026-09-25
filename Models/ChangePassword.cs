using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.Models
{
    /// <summary>Change-password form for a signed-in user.</summary>
    public class ChangePassword
    {
        [Required(ErrorMessage = "Podaj obecne hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Obecne hasło")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj nowe hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.Message)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Powtórz nowe hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        [Compare(nameof(NewPassword), ErrorMessage = "Potwierdzenie hasła nie zgadza się z nowym hasłem.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public static class PasswordPolicy
    {
        public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$";
        public const string Message = "Hasło musi zawierać małą i dużą literę, cyfrę, znak specjalny i mieć co najmniej 8 znaków.";
    }
}

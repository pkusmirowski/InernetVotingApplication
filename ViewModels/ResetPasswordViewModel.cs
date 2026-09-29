using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ViewModels;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Link do ustawienia hasła jest niepełny. Otwórz go ponownie z wiadomości e-mail.")]
    public Guid Token { get; set; }

    [Required(ErrorMessage = "Podaj nowe hasło")]
    [DataType(DataType.Password)]
    [Display(Name = "Nowe hasło")]
    [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.Message)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Powtórz nowe hasło")]
    [DataType(DataType.Password)]
    [Display(Name = "Powtórz nowe hasło")]
    [Compare(nameof(NewPassword), ErrorMessage = "Oba hasła muszą być takie same. Wpisz je ponownie.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

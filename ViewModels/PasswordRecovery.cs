using System.ComponentModel.DataAnnotations;
using InternetVotingApplication.Validation;

namespace InternetVotingApplication.ViewModels;

/// <summary>Request a password-reset link.</summary>
public class PasswordRecovery
{
    [Required(ErrorMessage = "Wpisz swój adres e-mail")]
    [StringLength(254, ErrorMessage = ValidationMessages.TooLong)]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail")]
    [Display(Name = "Adres e-mail")]
    public string Email { get; set; } = string.Empty;
}

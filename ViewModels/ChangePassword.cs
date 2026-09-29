using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ViewModels;

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
    [Display(Name = "Powtórz nowe hasło")]
    [Compare(nameof(NewPassword), ErrorMessage = "Oba hasła muszą być takie same. Wpisz je ponownie.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public static class PasswordPolicy
{
    public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$";
    public const string Message = "Hasło musi mieć co najmniej 8 znaków, w tym małą literę, dużą literę, cyfrę i znak specjalny (na przykład ! lub #).";
}

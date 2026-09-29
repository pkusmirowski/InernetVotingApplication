using System.ComponentModel.DataAnnotations;
using InternetVotingApplication.Validation;

namespace InternetVotingApplication.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Wpisz swoje imię")]
    [StringLength(50, ErrorMessage = ValidationMessages.TooLong)]
    [Display(Name = "Imię")]
    public string Imie { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wpisz swoje nazwisko")]
    [StringLength(50, ErrorMessage = ValidationMessages.TooLong)]
    [Display(Name = "Nazwisko")]
    public string Nazwisko { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wpisz swój numer PESEL")]
    [RegularExpression("^[0-9]{11}$", ErrorMessage = "Numer PESEL ma 11 cyfr. Wpisz go bez spacji i innych znaków.")]
    [Display(Name = "PESEL")]
    public string Pesel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wpisz swój adres e-mail")]
    [StringLength(254, ErrorMessage = ValidationMessages.TooLong)]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail")]
    [Display(Name = "Adres e-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wpisz swoją datę urodzenia")]
    [DataType(DataType.Date)]
    [Display(Name = "Data urodzenia")]
    [Age(18, ErrorMessage = "Głosować mogą tylko osoby pełnoletnie, czyli takie, które ukończyły 18 lat.")]
    public DateTime? DataUrodzenia { get; set; }

    [Required(ErrorMessage = "Podaj hasło")]
    [DataType(DataType.Password)]
    [Display(Name = "Hasło")]
    [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.Message)]
    public string Haslo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Powtórz hasło")]
    [DataType(DataType.Password)]
    [Display(Name = "Powtórz hasło")]
    [Compare(nameof(Haslo), ErrorMessage = "Oba hasła muszą być takie same. Wpisz je ponownie.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

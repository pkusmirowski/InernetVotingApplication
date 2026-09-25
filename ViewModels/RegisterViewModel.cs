using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Models;
using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Wpisz swoje imię")]
        [StringLength(50)]
        [Display(Name = "Imię")]
        public string Imie { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wpisz swoje nazwisko")]
        [StringLength(50)]
        [Display(Name = "Nazwisko")]
        public string Nazwisko { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wpisz swój numer PESEL")]
        [RegularExpression("^[0-9]{11}$", ErrorMessage = "PESEL musi składać się z 11 cyfr")]
        [Display(Name = "PESEL")]
        public string Pesel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wpisz swój adres e-mail")]
        [StringLength(254)]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail")]
        [Display(Name = "Adres e-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wpisz swoją datę urodzenia")]
        [DataType(DataType.Date)]
        [Display(Name = "Data urodzenia")]
        [Age(18, ErrorMessage = "Musisz być osobą pełnoletnią")]
        public DateTime? DataUrodzenia { get; set; }

        [Required(ErrorMessage = "Podaj hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = PasswordPolicy.Message)]
        public string Haslo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdź hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        [Compare(nameof(Haslo), ErrorMessage = "Hasła się nie zgadzają")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

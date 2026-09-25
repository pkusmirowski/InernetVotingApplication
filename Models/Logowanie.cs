using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.Models
{
    /// <summary>Sign-in form.</summary>
    public class Logowanie
    {
        [Required(ErrorMessage = "Wpisz swój adres e-mail")]
        [StringLength(254)]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail")]
        [Display(Name = "Adres e-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Haslo { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}

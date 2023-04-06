using System.ComponentModel.DataAnnotations;

namespace InernetVotingApplication.Models
{
    public class ChangePassword
    {
        [Required(ErrorMessage = "Wprowadź aktualne hasło")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Wprowadź nowe hasło")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Nowe hasło musi składać się z co najmniej 8 znaków, w tym przynajmniej jednej małej litery, jednej wielkiej litery i jednej cyfry.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Potwierdź nowe hasło")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Nowe hasło i potwierdzone hasło nie są takie same.")]
        public string ConfirmNewPassword { get; set; }
    }
}

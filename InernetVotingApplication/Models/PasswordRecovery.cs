using System.ComponentModel.DataAnnotations;

namespace InernetVotingApplication.Models
{
    public class PasswordRecovery
    {
        [Required(ErrorMessage = "Wpisz swój adres email")]
        [StringLength(89)]
        [Display(Name = "Adres Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Wpisz swój PESEL")]
        [StringLength(11)]
        [Display(Name = "Pesel")]
        public string Pesel { get; set; }
    }
}

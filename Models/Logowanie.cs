using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace InernetVotingApplication.Models
{
    public class Logowanie
    {
        [Required(ErrorMessage = "Wpisz swój numer dowodu osobistego")]
        [StringLength(6)]
        [Display(Name = "Nr dowodu osobistego")]
        public string NumerDowodu { get; set; }

        [Required(ErrorMessage = "Podaj hasło")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Haslo { get; set; }
    }
}
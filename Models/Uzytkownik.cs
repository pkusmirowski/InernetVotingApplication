using InernetVotingApplication.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace InernetVotingApplication.Models
{
    [Table("Uzytkownik")]
    public partial class Uzytkownik
    {
        public Uzytkownik()
        {
            Administrators = new HashSet<Administrator>();
            GlosUzytkownikas = new HashSet<GlosUzytkownika>();
        }

        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Required(ErrorMessage = "Wpisz swoje imię")]
        [Column("imie")]
        [StringLength(50)]
        [Display(Name = "Imię")]
        public string Imie { get; set; }
        [Required(ErrorMessage = "Wpisz swoje nazwisko")]
        [Column("nazwisko")]
        [StringLength(50)]
        [Display(Name = "Nazwisko")]
        public string Nazwisko { get; set; }
        [Required(ErrorMessage = "Wpisz swój numer PESEL")]
        [Column("pesel")]
        [StringLength(11)]
        [Display(Name = "PESEL")]
        public string Pesel { get; set; }
        [Required(ErrorMessage = "Wpisz swój adres Email")]
        [Column("email")]
        [StringLength(89)]
        [Display(Name = "Adres Email")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Wpisz swoją datę urodzenia")]
        [Column("dataUrodzenia", TypeName = "date")]
        [Display(Name = "Data urodzenia")]
        [DataType(DataType.Date)]
        [AgeAttribute(18, ErrorMessage = "Musisz być pełnoletni!")]
        public DateTime DataUrodzenia { get; set; }
        [Required(ErrorMessage = "Podaj hasło")]
        [Column("haslo")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#$^+=!*()@%&]).{8,}$",
         ErrorMessage = "Hasło musi zawierać: małą i dużą litere, cyfre, specjalny symbol, 8 znaków")]
        public string Haslo { get; set; }
        [NotMapped]
        [Compare("Haslo", ErrorMessage = "Hasła się nie zgadzają!")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        public string ConfirmPassword { get; set; }
        [Column("jestAktywne")]
        public bool JestAktywne { get; set; }
        [Column("kodAktywacyjny")]
        public Guid KodAktywacyjny { get; set; }
        [InverseProperty(nameof(Administrator.IdUzytkownikNavigation))]
        public virtual ICollection<Administrator> Administrators { get; set; }
        [InverseProperty(nameof(GlosUzytkownika.IdUzytkownikNavigation))]
        public virtual ICollection<GlosUzytkownika> GlosUzytkownikas { get; set; }
    }
}

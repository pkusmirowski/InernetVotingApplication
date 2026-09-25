using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>
    /// Registered voter account. Form input is bound to <see cref="ViewModels.RegisterViewModel"/>,
    /// never directly to this entity.
    /// </summary>
    [Table("Uzytkownik")]
    public class Uzytkownik
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("imie")]
        [StringLength(50)]
        public string Imie { get; set; } = null!;

        [Column("nazwisko")]
        [StringLength(50)]
        public string Nazwisko { get; set; } = null!;

        [Column("pesel")]
        [StringLength(11)]
        public string Pesel { get; set; } = null!;

        [Column("email")]
        [StringLength(254)]
        public string Email { get; set; } = null!;

        [Column("dataUrodzenia", TypeName = "date")]
        public DateTime DataUrodzenia { get; set; }

        /// <summary>BCrypt hash of the password.</summary>
        [Column("haslo")]
        [StringLength(100)]
        public string Haslo { get; set; } = null!;

        [Column("jestAktywne")]
        public bool JestAktywne { get; set; }

        /// <summary>Activation code sent by e-mail; cleared once the account is activated.</summary>
        [Column("kodAktywacyjny")]
        public Guid? KodAktywacyjny { get; set; }

        [Column("tokenResetuHasla")]
        public Guid? TokenResetuHasla { get; set; }

        [Column("tokenResetuWygasa")]
        public DateTime? TokenResetuWygasa { get; set; }

        [Column("nieudaneLogowania")]
        public int NieudaneLogowania { get; set; }

        [Column("zablokowaneDo")]
        public DateTime? ZablokowaneDo { get; set; }

        [Column("dataRejestracji")]
        public DateTime DataRejestracji { get; set; }

        public ICollection<Administrator> Administrators { get; set; } = new HashSet<Administrator>();

        public ICollection<GlosUzytkownika> GlosUzytkownikas { get; set; } = new HashSet<GlosUzytkownika>();
    }
}

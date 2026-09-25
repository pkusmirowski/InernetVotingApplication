using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>
    /// Outbox row: an e-mail persisted in the same transaction as the business change that produced it.
    /// </summary>
    [Table("WiadomoscEmail")]
    public class WiadomoscEmail
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("odbiorca")]
        [StringLength(254)]
        public string Odbiorca { get; set; } = null!;

        [Column("temat")]
        [StringLength(200)]
        public string Temat { get; set; } = null!;

        [Column("tresc")]
        public string Tresc { get; set; } = null!;

        [Column("utworzono")]
        public DateTime Utworzono { get; set; }

        [Column("wyslano")]
        public DateTime? Wyslano { get; set; }

        [Column("proby")]
        public int Proby { get; set; }

        [Column("nastepnaProba")]
        public DateTime? NastepnaProba { get; set; }

        [Column("ostatniBlad")]
        [StringLength(1000)]
        public string? OstatniBlad { get; set; }
    }
}

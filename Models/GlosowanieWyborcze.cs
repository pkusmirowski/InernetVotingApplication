using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>
    /// A single block of an election's hash chain. Deliberately carries no reference to the voter.
    /// </summary>
    [Table("GlosowanieWyborcze")]
    public class GlosowanieWyborcze
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Zero-based position of the block within the election's chain.</summary>
        [Column("indeks")]
        public int Indeks { get; set; }

        [Column("id_kandydat")]
        public int IdKandydat { get; set; }

        [Column("id_wybory")]
        public int IdWybory { get; set; }

        [Column("id_poprzednie")]
        public int? IdPoprzednie { get; set; }

        [Column("znacznikCzasu")]
        public DateTime ZnacznikCzasu { get; set; }

        /// <summary>Random 128-bit value (hex) that makes every block hash unpredictable.</summary>
        [Column("nonce")]
        [StringLength(32)]
        public string Nonce { get; set; } = null!;

        /// <summary>SHA-256 of the canonical block data, upper-case hex.</summary>
        [Column("hash")]
        [StringLength(64)]
        public string Hash { get; set; } = null!;

        [ForeignKey(nameof(IdKandydat))]
        public Kandydat IdKandydatNavigation { get; set; } = null!;

        [ForeignKey(nameof(IdWybory))]
        public DataWyborow IdWyboryNavigation { get; set; } = null!;
    }
}

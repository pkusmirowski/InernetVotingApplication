using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>
    /// A published snapshot of an election chain head (an "anchor"). The row is the local record;
    /// the copy sent outside the system (e-mail to the committee) is what makes history rewriting detectable.
    /// </summary>
    [Table("KotwicaLancucha")]
    public class KotwicaLancucha
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_wybory")]
        public int IdWybory { get; set; }

        [Column("liczbaBlokow")]
        public int LiczbaBlokow { get; set; }

        [Column("hashGlowy")]
        [StringLength(64)]
        public string? HashGlowy { get; set; }

        [Column("data")]
        public DateTime Data { get; set; }

        /// <summary>Why the anchor was published: Periodic, ElectionEnded, Manual.</summary>
        [Column("powod")]
        [StringLength(30)]
        public string Powod { get; set; } = null!;

        [Column("odbiorcy")]
        [StringLength(500)]
        public string? Odbiorcy { get; set; }

        /// <summary>Signature of "electionId|blockCount|headHash|timestamp" made with the block-signing key.</summary>
        [Column("podpis")]
        [StringLength(128)]
        public string Podpis { get; set; } = null!;

        [ForeignKey(nameof(IdWybory))]
        public DataWyborow IdWyboryNavigation { get; set; } = null!;
    }
}

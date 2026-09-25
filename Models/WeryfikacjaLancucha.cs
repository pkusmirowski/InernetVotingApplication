using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>Result of one full verification run of an election chain.</summary>
    [Table("WeryfikacjaLancucha")]
    public class WeryfikacjaLancucha
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_wybory")]
        public int IdWybory { get; set; }

        [Column("data")]
        public DateTime Data { get; set; }

        [Column("poprawny")]
        public bool Poprawny { get; set; }

        [Column("liczbaBlokow")]
        public int LiczbaBlokow { get; set; }

        [Column("hashGlowy")]
        [StringLength(64)]
        public string? HashGlowy { get; set; }

        /// <summary>What started the run: Background, Manual, Results, Vote.</summary>
        [Column("wyzwalacz")]
        [StringLength(30)]
        public string Wyzwalacz { get; set; } = null!;

        [Column("szczegoly")]
        [StringLength(2000)]
        public string? Szczegoly { get; set; }

        [ForeignKey(nameof(IdWybory))]
        public DataWyborow IdWyboryNavigation { get; set; } = null!;
    }
}

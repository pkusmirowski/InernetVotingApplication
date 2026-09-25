using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>Append-only record of administrative actions and chain events.</summary>
    [Table("DziennikAudytu")]
    public class DziennikAudytu
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("data")]
        public DateTime Data { get; set; }

        [Column("id_uzytkownik")]
        public int? IdUzytkownik { get; set; }

        [Column("akcja")]
        [StringLength(60)]
        public string Akcja { get; set; } = null!;

        [Column("szczegoly")]
        [StringLength(2000)]
        public string? Szczegoly { get; set; }
    }
}

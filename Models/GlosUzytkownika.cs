using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>
    /// Records that a voter has taken part in an election. Holds no information about the choice made.
    /// </summary>
    [Table("GlosUzytkownika")]
    public class GlosUzytkownika
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_uzytkownik")]
        public int IdUzytkownik { get; set; }

        [Column("id_wybory")]
        public int IdWybory { get; set; }

        [Column("dataOddania")]
        public DateTime DataOddania { get; set; }

        [ForeignKey(nameof(IdUzytkownik))]
        public Uzytkownik IdUzytkownikNavigation { get; set; } = null!;

        [ForeignKey(nameof(IdWybory))]
        public DataWyborow IdWyboryNavigation { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    [Table("Kandydat")]
    public class Kandydat
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

        [Column("id_wybory")]
        public int IdWybory { get; set; }

        [ForeignKey(nameof(IdWybory))]
        public DataWyborow IdWyboryNavigation { get; set; } = null!;

        public ICollection<GlosowanieWyborcze> GlosowanieWyborczes { get; set; } = new HashSet<GlosowanieWyborcze>();
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    [Table("Kandydat")]
    public partial class Kandydat
    {
        public Kandydat()
        {
            GlosowanieWyborczes = new HashSet<GlosowanieWyborcze>();
        }

        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Required]
        [Column("imie")]
        [StringLength(50)]
        public string Imie { get; set; }
        [Required]
        [Column("nazwisko")]
        [StringLength(50)]
        public string Nazwisko { get; set; }
        [Column("id_wybory")]
        public int IdWybory { get; set; }

        [ForeignKey(nameof(IdWybory))]
        [InverseProperty(nameof(DataWyborow.Kandydats))]
        public virtual DataWyborow IdWyboryNavigation { get; set; }
        [InverseProperty(nameof(GlosowanieWyborcze.IdKandydatNavigation))]
        public virtual ICollection<GlosowanieWyborcze> GlosowanieWyborczes { get; set; }
    }
}
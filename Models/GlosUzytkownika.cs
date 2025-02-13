using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    [Table("GlosUzytkownika")]
    public partial class GlosUzytkownika
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_uzytkownik")]
        public int IdUzytkownik { get; set; }

        [Column("id_wybory")]
        public int IdWybory { get; set; }

        [Column("glos")]
        public bool Glos { get; set; }

        [ForeignKey(nameof(IdUzytkownik))]
        [InverseProperty(nameof(Uzytkownik.GlosUzytkownikas))]
        public virtual Uzytkownik IdUzytkownikNavigation { get; set; }

        [ForeignKey(nameof(IdWybory))]
        [InverseProperty(nameof(DataWyborow.GlosUzytkownikas))]
        public virtual DataWyborow IdWyboryNavigation { get; set; }
    }
}
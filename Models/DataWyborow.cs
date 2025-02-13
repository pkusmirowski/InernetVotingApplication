using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    [Table("DataWyborow")]
    public partial class DataWyborow
    {
        public DataWyborow()
        {
            GlosUzytkownikas = new HashSet<GlosUzytkownika>();
            GlosowanieWyborczes = new HashSet<GlosowanieWyborcze>();
            Kandydats = new HashSet<Kandydat>();
        }

        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("dataRozpoczecia", TypeName = "datetime")]
        public DateTime DataRozpoczecia { get; set; }

        [Column("dataZakonczenia", TypeName = "datetime")]
        public DateTime DataZakonczenia { get; set; }

        [Required]
        [Column("opis")]
        public string Opis { get; set; }

        [InverseProperty(nameof(GlosUzytkownika.IdWyboryNavigation))]
        public virtual ICollection<GlosUzytkownika> GlosUzytkownikas { get; set; }

        [InverseProperty(nameof(GlosowanieWyborcze.IdWyboryNavigation))]
        public virtual ICollection<GlosowanieWyborcze> GlosowanieWyborczes { get; set; }

        [InverseProperty(nameof(Kandydat.IdWyboryNavigation))]
        public virtual ICollection<Kandydat> Kandydats { get; set; }
    }
}
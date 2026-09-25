using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>
    /// An election: a named voting window with its own candidates and its own hash chain.
    /// </summary>
    [Table("DataWyborow")]
    public class DataWyborow
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("dataRozpoczecia")]
        public DateTime DataRozpoczecia { get; set; }

        [Column("dataZakonczenia")]
        public DateTime DataZakonczenia { get; set; }

        [Column("opis")]
        [StringLength(200)]
        public string Opis { get; set; } = null!;

        public ICollection<GlosUzytkownika> GlosUzytkownikas { get; set; } = new HashSet<GlosUzytkownika>();

        public ICollection<GlosowanieWyborcze> GlosowanieWyborczes { get; set; } = new HashSet<GlosowanieWyborcze>();

        public ICollection<Kandydat> Kandydats { get; set; } = new HashSet<Kandydat>();

        public ElectionStatus GetStatus(DateTime now) => GetStatus(DataRozpoczecia, DataZakonczenia, now);

        public static ElectionStatus GetStatus(DateTime start, DateTime end, DateTime now)
        {
            if (now < start)
            {
                return ElectionStatus.Upcoming;
            }

            return now > end ? ElectionStatus.Ended : ElectionStatus.Ongoing;
        }
    }
}

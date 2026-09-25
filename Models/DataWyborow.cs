using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    /// <summary>
    /// An election: a named voting window with its own candidates and its own hash chain.
    /// The row also carries the chain head so that appending a block does not require reading the whole chain.
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

        /// <summary>Hash of the last block, null for an empty chain.</summary>
        [Column("hashGlowy")]
        [StringLength(64)]
        public string? HashGlowy { get; set; }

        [Column("liczbaBlokow")]
        public int LiczbaBlokow { get; set; }

        /// <summary>Manually incremented concurrency token (portable across SQL Server and SQLite).</summary>
        [Column("wersja")]
        [ConcurrencyCheck]
        public int Wersja { get; set; }

        public ICollection<GlosUzytkownika> GlosUzytkownikas { get; set; } = new HashSet<GlosUzytkownika>();

        public ICollection<GlosowanieWyborcze> GlosowanieWyborczes { get; set; } = new HashSet<GlosowanieWyborcze>();

        public ICollection<Kandydat> Kandydats { get; set; } = new HashSet<Kandydat>();

        public ICollection<KotwicaLancucha> Kotwice { get; set; } = new HashSet<KotwicaLancucha>();

        public ICollection<WeryfikacjaLancucha> Weryfikacje { get; set; } = new HashSet<WeryfikacjaLancucha>();

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

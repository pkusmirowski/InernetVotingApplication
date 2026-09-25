using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    [Table("Administrator")]
    public class Administrator
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_uzytkownik")]
        public int IdUzytkownik { get; set; }

        [ForeignKey(nameof(IdUzytkownik))]
        public Uzytkownik IdUzytkownikNavigation { get; set; } = null!;
    }
}

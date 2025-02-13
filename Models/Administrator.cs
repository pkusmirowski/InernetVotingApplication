using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetVotingApplication.Models
{
    [Table("Administrator")]
    public partial class Administrator
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_uzytkownik")]
        public int IdUzytkownik { get; set; }

        [ForeignKey(nameof(IdUzytkownik))]
        [InverseProperty(nameof(Uzytkownik.Administrators))]
        public virtual Uzytkownik IdUzytkownikNavigation { get; set; }
    }
}
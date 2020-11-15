using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace InernetVotingApplication.Models
{
    [Table("GlosowanieWyborcze")]
    public partial class GlosowanieWyborcze
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("id_kandydat")]
        public int IdKandydat { get; set; }
        [Column("id_wybory")]
        public int IdWybory { get; set; }
        [Column("glos")]
        public bool Glos { get; set; }
        [Required]
        [Column("hash")]
        public string Hash { get; set; }
        [Column("jestPoprawny")]
        public bool JestPoprawny { get; set; }

        [ForeignKey(nameof(IdKandydat))]
        [InverseProperty(nameof(Kandydat.GlosowanieWyborczes))]
        public virtual Kandydat IdKandydatNavigation { get; set; }
        [ForeignKey(nameof(IdWybory))]
        [InverseProperty(nameof(DataWyborow.GlosowanieWyborczes))]
        public virtual DataWyborow IdWyboryNavigation { get; set; }
    }
}

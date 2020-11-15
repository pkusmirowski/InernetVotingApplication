using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InernetVotingApplication.Models
{
    public class DataWyborowItemViewModel
    {
        public int Id { get; set; }
        public DateTime DataRozpoczecia { get; set; }
        public DateTime DataZakonczenia { get; set; }
        public string Opis { get; set; }
    }
}

using System;

namespace InternetVotingApplication.Models
{
    public class DataWyborowItemViewModel
    {
        public int Id { get; set; }
        public DateTime DataRozpoczecia { get; set; }
        public DateTime DataZakonczenia { get; set; }
        public string Opis { get; set; }
        public int Type { get; set; }
    }
}

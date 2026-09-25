using InternetVotingApplication.Models;

namespace InternetVotingApplication.ViewModels
{
    public class DataWyborowItemViewModel
    {
        public int Id { get; set; }

        public string Opis { get; set; } = string.Empty;

        public DateTime DataRozpoczecia { get; set; }

        public DateTime DataZakonczenia { get; set; }

        public ElectionStatus Status { get; set; }

        public bool HasVoted { get; set; }

        public int CandidateCount { get; set; }
    }
}

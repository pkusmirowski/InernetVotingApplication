using InternetVotingApplication.Models;

namespace InternetVotingApplication.ViewModels
{
    /// <summary>Results of a finished election together with the state of its hash chain.</summary>
    public class GlosowanieWyborczeViewModel
    {
        public int ElectionId { get; set; }

        public string ElectionName { get; set; } = string.Empty;

        public DateTime DataRozpoczecia { get; set; }

        public DateTime DataZakonczenia { get; set; }

        public ElectionStatus Status { get; set; }

        public bool HasVoted { get; set; }

        public int TotalVotes { get; set; }

        public IReadOnlyList<GlosowanieWyborczeItemViewModel> Rows { get; set; } = [];

        public bool ChainValid { get; set; }

        public int BlockCount { get; set; }

        public string? HeadHash { get; set; }
    }
}

namespace InternetVotingApplication.ViewModels
{
    /// <summary>One row of the results table.</summary>
    public class GlosowanieWyborczeItemViewModel
    {
        public int IdKandydat { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string CandidateSurname { get; set; } = string.Empty;

        public int CountedVotes { get; set; }

        public double CountedVotesPercentage { get; set; }
    }
}

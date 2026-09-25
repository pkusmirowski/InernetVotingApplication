namespace InternetVotingApplication.ViewModels
{
    public class CandidateListItemViewModel
    {
        public int Id { get; set; }

        public string Imie { get; set; } = string.Empty;

        public string Nazwisko { get; set; } = string.Empty;

        public int ElectionId { get; set; }

        public string ElectionName { get; set; } = string.Empty;

        public int VoteCount { get; set; }
    }

    public class CandidateListViewModel
    {
        public int? SelectedElectionId { get; set; }

        public IReadOnlyList<ElectionOptionViewModel> Elections { get; set; } = [];

        public IReadOnlyList<CandidateListItemViewModel> Candidates { get; set; } = [];
    }
}

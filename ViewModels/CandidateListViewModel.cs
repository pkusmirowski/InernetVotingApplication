namespace InternetVotingApplication.ViewModels;

public class CandidateListItemViewModel
{
    public int Id { get; set; }

    public string Imie { get; set; } = string.Empty;

    public string Nazwisko { get; set; } = string.Empty;

    public int ElectionId { get; set; }

    public string ElectionName { get; set; } = string.Empty;

    /// <summary>Votes for the candidate; null until the election ends, so no partial results are shown.</summary>
    public int? VoteCount { get; set; }

    /// <summary>Candidates can be removed only before voting starts.</summary>
    public bool CanDelete { get; set; }
}

public class CandidateListViewModel
{
    public int? SelectedElectionId { get; set; }

    public IReadOnlyList<ElectionOptionViewModel> Elections { get; set; } = [];

    public IReadOnlyList<CandidateListItemViewModel> Candidates { get; set; } = [];
}

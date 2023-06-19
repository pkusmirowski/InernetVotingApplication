using System.Collections.Generic;

namespace InternetVotingApplication.ViewModels
{
    public class GlosowanieWyborczeViewModel
    {
        public GlosowanieWyborczeItemViewModel SearchCandidate { get; set; }
        public List<GlosowanieWyborczeItemViewModel> GetElectionVotes { get; set; }
        public string Text { get; set; }
    }
}

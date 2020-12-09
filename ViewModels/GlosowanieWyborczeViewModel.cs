using System.Collections.Generic;

namespace InernetVotingApplication.ViewModels
{
    public class GlosowanieWyborczeViewModel
    {
        public GlosowanieWyborczeItemViewModel SearchCandidate { get; set; }
        public List<GlosowanieWyborczeItemViewModel> GetElectionVotes { get; set; }
        public string Text { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InernetVotingApplication.ViewModels
{
    public class KandydatViewModel
    {
        public IEnumerable<KandydatItemViewModel> ElectionCandidates { get; set; }
    }
}

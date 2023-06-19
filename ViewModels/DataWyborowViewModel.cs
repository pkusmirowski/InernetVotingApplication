using InternetVotingApplication.Models;
using System.Collections.Generic;

namespace InternetVotingApplication.ViewModels
{
    public class DataWyborowViewModel
    {
        public IEnumerable<DataWyborowItemViewModel> ElectionDates { get; set; }
    }
}

using InernetVotingApplication.Models;
using System.Collections.Generic;

namespace InernetVotingApplication.ViewModels
{
    public class DataWyborowViewModel
    {
        public IEnumerable<DataWyborowItemViewModel> ElectionDates { get; set; }
    }
}

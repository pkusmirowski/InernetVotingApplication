using InernetVotingApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InernetVotingApplication.ViewModels
{
    public class DataWyborowViewModel
    {
        public IEnumerable<DataWyborowItemViewModel> ElectionDates { get; set; }
    }
}

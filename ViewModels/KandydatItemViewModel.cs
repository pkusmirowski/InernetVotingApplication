using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InernetVotingApplication.ViewModels
{
    public class KandydatItemViewModel
    {
        public int Id { get; set; }

        public string Imie { get; set; }

        public string Nazwisko { get; set; }

        public int IdWybory { get; set; }
    }
}

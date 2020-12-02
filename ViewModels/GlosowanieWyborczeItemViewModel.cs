namespace InernetVotingApplication.ViewModels
{
    public class GlosowanieWyborczeItemViewModel
    {
        public int IdKandydat { get; set; }
        public int IdWybory { get; set; }
        public string Hash { get; set; }

        public string CandidateName { get; set; }
        public string CandidateSurname { get; set; }
        public string ElectionDesc { get; set; }
    }
}

namespace InernetVotingApplication.Blockchain
{
    public static class BlockHelper
    {
        public static string VoteData(int idKandydat, int idWybory, bool glos, string previousBlockHash)
        {
            return $"{idKandydat}{idWybory}{glos}{previousBlockHash}";
        }
    }
}

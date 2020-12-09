namespace InernetVotingApplication.Blockchain
{
    public static class BlockHelper
    {
        public static string VoteData(int IdKandydat, int IdWybory, bool Glos, string previousBlockHash)
        {
            return $"{IdKandydat}{IdWybory}{Glos}{previousBlockHash}";
        }
    }
}

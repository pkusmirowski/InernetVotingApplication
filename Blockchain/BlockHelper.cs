namespace InernetVotingApplication.Blockchain
{
    public class BlockHelper
    {
        public static string VoteData(int IdKandydat, int IdWybory, bool Glos, string previousBlockHash)
        {
            return $"{IdKandydat}{IdWybory}{Glos}{previousBlockHash}";
        }
    }
}

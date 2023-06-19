namespace InternetVotingApplication.Blockchain
{
    public static class BlockHelper
    {
        public static string VoteData(int candidateId, int electionId, bool vote, string previousBlockHash)
        {
            return $"{candidateId}{electionId}{vote}{previousBlockHash}";
        }
    }
}

namespace InternetVotingApplication.Blockchain
{
    public static class BlockHelper
    {
        /// <summary>
        /// Generates a string representation of the vote data for hashing.
        /// </summary>
        /// <param name="candidateId">The ID of the candidate.</param>
        /// <param name="electionId">The ID of the election.</param>
        /// <param name="vote">The vote value.</param>
        /// <param name="previousBlockHash">The hash of the previous block.</param>
        /// <returns>A string representation of the vote data.</returns>
        public static string VoteData(int candidateId, int electionId, bool vote, string previousBlockHash)
        {
            return $"{candidateId}{electionId}{vote}{previousBlockHash}";
        }
    }
}
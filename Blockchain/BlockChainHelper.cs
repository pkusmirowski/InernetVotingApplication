using InternetVotingApplication.Models;
using System.Collections.Generic;
using System.Linq;

namespace InternetVotingApplication.Blockchain
{
    public static class BlockChainHelper
    {
        /// <summary>
        /// Verifies the integrity of the blockchain by checking the hashes of each block.
        /// </summary>
        /// <param name="listOfPreviousElectionVotes">The list of previous election votes to verify.</param>
        public static void VerifyBlockChain(IList<GlosowanieWyborcze> listOfPreviousElectionVotes)
        {
            string previousHash = null;
            foreach (var currentBlock in listOfPreviousElectionVotes.OrderBy(c => c.Id))
            {
                var previousBlock = listOfPreviousElectionVotes.SingleOrDefault(c => c.Id == currentBlock.IdPoprzednie);
                var blockText = BlockHelper.VoteData(
                    currentBlock.IdKandydat,
                    currentBlock.IdWybory,
                    currentBlock.Glos,
                    previousHash);

                var blockHash = HashHelper.Hash(blockText);

                // Check current block hashes and previous block hashes to ensure integrity
                currentBlock.JestPoprawny = blockHash == currentBlock.Hash && previousHash == previousBlock?.Hash;

                previousHash = blockHash;
            }
        }
    }
}
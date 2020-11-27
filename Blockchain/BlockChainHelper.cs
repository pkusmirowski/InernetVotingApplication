using InernetVotingApplication.Models;
using System.Collections.Generic;
using System.Linq;

namespace InernetVotingApplication.Blockchain
{
    public class BlockChainHelper
    {
        public static void VerifyBlockChain(IList<GlosowanieWyborcze> listOfPreviousElectionVotes)
        {
            string previousHash = null;
            foreach (var value in listOfPreviousElectionVotes.OrderBy(c => c.Id))
            {
                var previousBlock = listOfPreviousElectionVotes.SingleOrDefault(c => c.Id == value.IdPoprzednie);
                var blockText = BlockHelper.VoteData(
                    value.IdKandydat,
                    value.IdWybory,
                    value.Glos,
                    previousHash);

                var blockHash = HashHelper.Hash(blockText);

                // check current block hashes, and previous block hashes, since
                // it could have been modified in DB, ie checking the chain by two blocks at a time
                value.JestPoprawny = blockHash == value.Hash && previousHash == previousBlock?.Hash;

                previousHash = blockHash;
            }
        }
    }
}

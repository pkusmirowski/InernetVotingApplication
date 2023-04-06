using InernetVotingApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InernetVotingApplication.Blockchain
{
    public static class BlockChainHelper
    {
        private const int FirstBlockId = 0;

        public static bool VerifyBlockChain(IEnumerable<GlosowanieWyborcze> listOfPreviousElectionVotes)
        {
            if (!listOfPreviousElectionVotes.Any())
            {
                throw new ArgumentException("Lista poprzednich głosowań nie może być pusta.", nameof(listOfPreviousElectionVotes));
            }

            string previousHash = "";

            foreach (GlosowanieWyborcze value in listOfPreviousElectionVotes.OrderBy(c => c.Id))
            {
                if (value.Id != FirstBlockId && listOfPreviousElectionVotes.SingleOrDefault(c => c.Id == value.IdPoprzednie)?.Hash != previousHash)
                {
                    return false;
                }

                if (HashHelper.Hash(BlockHelper.VoteData(value.IdKandydat, value.IdWybory, value.Glos, previousHash)) != value.Hash)
                {
                    return false;
                }

                previousHash = value.Hash;
            }

            return true;
        }
    }
}

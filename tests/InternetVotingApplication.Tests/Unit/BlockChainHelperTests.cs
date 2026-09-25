using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Models;

namespace InternetVotingApplication.Tests.Unit
{
    public class BlockChainHelperTests
    {
        private static List<GlosowanieWyborcze> BuildChain(int length, int electionId = 1)
        {
            var chain = new List<GlosowanieWyborcze>();
            GlosowanieWyborcze? previous = null;
            for (int i = 0; i < length; i++)
            {
                var block = new GlosowanieWyborcze
                {
                    Id = i + 100,
                    Indeks = i,
                    IdKandydat = (i % 3) + 1,
                    IdWybory = electionId,
                    IdPoprzednie = previous?.Id,
                    ZnacznikCzasu = new DateTime(2026, 1, 1).AddMinutes(i),
                    Nonce = BlockHelper.NewNonce(),
                };
                block.Hash = BlockHelper.ComputeHash(block, previous?.Hash);
                block.Podpis = TestData.Signer.Sign(block.Hash);
                block.IdKlucza = TestData.Signer.KeyId;
                chain.Add(block);
                previous = block;
            }

            return chain;
        }

        [Fact]
        public void Empty_chain_is_valid()
        {
            var result = BlockChainHelper.VerifyBlockChain([]);
            Assert.True(result.IsValid);
            Assert.Equal(0, result.BlockCount);
            Assert.Null(result.HeadHash);
        }

        [Fact]
        public void Untouched_chain_is_valid()
        {
            var chain = BuildChain(10);
            var result = BlockChainHelper.VerifyBlockChain(chain);
            Assert.True(result.IsValid);
            Assert.Equal(10, result.BlockCount);
            Assert.Equal(chain[^1].Hash, result.HeadHash);
        }

        [Fact]
        public void Order_of_input_does_not_matter()
        {
            var chain = BuildChain(5);
            chain.Reverse();
            Assert.True(BlockChainHelper.VerifyBlockChain(chain).IsValid);
        }

        [Fact]
        public void Changing_a_vote_invalidates_that_block()
        {
            var chain = BuildChain(5);
            chain[2].IdKandydat = 99;

            var result = BlockChainHelper.VerifyBlockChain(chain);

            Assert.False(result.IsValid);
            Assert.Contains(chain[2].Id, result.InvalidBlockIds);
        }

        [Fact]
        public void Recomputing_a_tampered_hash_breaks_the_next_block()
        {
            var chain = BuildChain(5);
            chain[2].IdKandydat = 99;
            chain[2].Hash = BlockHelper.ComputeHash(chain[2], chain[1].Hash);

            var result = BlockChainHelper.VerifyBlockChain(chain);

            Assert.False(result.IsValid);
            Assert.DoesNotContain(chain[2].Id, result.InvalidBlockIds);
            Assert.Contains(chain[3].Id, result.InvalidBlockIds);
        }

        [Fact]
        public void Removing_a_block_is_detected()
        {
            var chain = BuildChain(5);
            chain.RemoveAt(1);

            var result = BlockChainHelper.VerifyBlockChain(chain);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Signatures_are_verified_when_a_signer_is_supplied()
        {
            var chain = BuildChain(3);
            Assert.True(BlockChainHelper.VerifyBlockChain(chain, TestData.Signer).IsValid);

            chain[1].Podpis = chain[0].Podpis;
            var result = BlockChainHelper.VerifyBlockChain(chain, TestData.Signer);

            Assert.False(result.IsValid);
            Assert.True(result.HashesValid);
            Assert.Equal([chain[1].Id], result.InvalidSignatureBlockIds);

            var otherKey = EcdsaBlockSigner.Generate();
            Assert.False(BlockChainHelper.VerifyBlockChain(chain, otherKey).SignaturesValid);
        }

        [Fact]
        public void Head_state_check_detects_mismatch()
        {
            var chain = BuildChain(2);
            var election = new DataWyborow { LiczbaBlokow = 2, HashGlowy = chain[1].Hash };
            Assert.True(BlockChainHelper.HeadMatches(election, chain[1]));
            Assert.False(BlockChainHelper.HeadMatches(election, chain[0]));
            Assert.False(BlockChainHelper.HeadMatches(new DataWyborow(), chain[1]));
            Assert.True(BlockChainHelper.HeadMatches(new DataWyborow(), null));
            Assert.False(BlockChainHelper.HeadMatches(election, null));
        }

        [Fact]
        public void Swapping_two_blocks_is_detected()
        {
            var chain = BuildChain(4);
            (chain[1].Indeks, chain[2].Indeks) = (chain[2].Indeks, chain[1].Indeks);

            Assert.False(BlockChainHelper.VerifyBlockChain(chain).IsValid);
        }
    }
}

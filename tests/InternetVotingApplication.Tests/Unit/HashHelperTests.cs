using InternetVotingApplication.Blockchain;

namespace InternetVotingApplication.Tests.Unit
{
    public class HashHelperTests
    {
        [Fact]
        public void Produces_known_sha256_digest()
        {
            Assert.Equal("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD", HashHelper.Hash("abc"));
        }

        [Fact]
        public void Hash_is_64_hex_characters()
        {
            var hash = HashHelper.Hash("anything");
            Assert.Equal(64, hash.Length);
            Assert.Matches("^[0-9A-F]{64}$", hash);
        }

        [Fact]
        public void Vote_data_fields_are_separated()
        {
            var ts = new DateTime(2026, 1, 1);
            var a = BlockHelper.VoteData(0, 1, 12, ts, "N", null);
            var b = BlockHelper.VoteData(0, 11, 2, ts, "N", null);
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Nonce_is_32_hex_characters_and_random()
        {
            var first = BlockHelper.NewNonce();
            var second = BlockHelper.NewNonce();
            Assert.Matches("^[0-9A-F]{32}$", first);
            Assert.NotEqual(first, second);
        }
    }
}

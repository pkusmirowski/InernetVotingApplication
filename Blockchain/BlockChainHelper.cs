using InternetVotingApplication.Models;

namespace InternetVotingApplication.Blockchain
{
    public sealed record ChainVerificationResult(bool IsValid, int BlockCount, string? HeadHash, IReadOnlyList<int> InvalidBlockIds)
    {
        public static ChainVerificationResult Empty { get; } = new(true, 0, null, []);
    }

    public static class BlockChainHelper
    {
        /// <summary>
        /// Verifies the integrity of one election's chain: every block must have the expected index,
        /// point to the previous block and carry a hash computed over its own data plus the previous hash.
        /// Runs in O(n).
        /// </summary>
        public static ChainVerificationResult VerifyBlockChain(IEnumerable<GlosowanieWyborcze> blocks)
        {
            ArgumentNullException.ThrowIfNull(blocks);

            var ordered = blocks.OrderBy(b => b.Indeks).ThenBy(b => b.Id).ToList();
            var invalid = new List<int>();
            string? previousHash = null;
            int? previousId = null;

            for (int i = 0; i < ordered.Count; i++)
            {
                var block = ordered[i];
                var expectedHash = BlockHelper.ComputeHash(block, previousHash);

                bool ok = block.Indeks == i
                    && block.IdPoprzednie == previousId
                    && string.Equals(block.Hash, expectedHash, StringComparison.OrdinalIgnoreCase);

                if (!ok)
                {
                    invalid.Add(block.Id);
                }

                previousHash = block.Hash;
                previousId = block.Id;
            }

            return new ChainVerificationResult(invalid.Count == 0, ordered.Count, previousHash, invalid);
        }
    }
}

using InternetVotingApplication.Models;

namespace InternetVotingApplication.Blockchain
{
    public sealed record ChainVerificationResult(
        bool IsValid,
        int BlockCount,
        string? HeadHash,
        IReadOnlyList<int> InvalidBlockIds,
        IReadOnlyList<int> InvalidSignatureBlockIds)
    {
        public static ChainVerificationResult Empty { get; } = new(true, 0, null, [], []);

        public bool HashesValid => InvalidBlockIds.Count == 0;

        public bool SignaturesValid => InvalidSignatureBlockIds.Count == 0;
    }

    public static class BlockChainHelper
    {
        /// <summary>
        /// Verifies the integrity of one election's chain in O(n): every block must have the expected index,
        /// point to the previous block, carry a hash computed over its own data plus the previous hash and,
        /// when a <paramref name="signer"/> is supplied, a valid signature of that hash.
        /// </summary>
        public static ChainVerificationResult VerifyBlockChain(IEnumerable<GlosowanieWyborcze> blocks, IBlockSigner? signer = null)
        {
            ArgumentNullException.ThrowIfNull(blocks);

            var ordered = blocks.OrderBy(b => b.Indeks).ThenBy(b => b.Id).ToList();
            var invalid = new List<int>();
            var invalidSignatures = new List<int>();
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

                if (signer != null && !signer.Verify(block.Hash, block.Podpis))
                {
                    invalidSignatures.Add(block.Id);
                }

                previousHash = block.Hash;
                previousId = block.Id;
            }

            return new ChainVerificationResult(
                invalid.Count == 0 && invalidSignatures.Count == 0,
                ordered.Count,
                previousHash,
                invalid,
                invalidSignatures);
        }

        /// <summary>
        /// Checks the stored head state of an election against the actual last block. Cheap (two rows) and run
        /// before every append; the full verification runs in the background.
        /// </summary>
        public static bool HeadMatches(DataWyborow election, GlosowanieWyborcze? head)
        {
            ArgumentNullException.ThrowIfNull(election);
            if (head == null)
            {
                return election.LiczbaBlokow == 0 && election.HashGlowy == null;
            }

            return election.LiczbaBlokow == head.Indeks + 1
                && string.Equals(election.HashGlowy, head.Hash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Canonical text signed for an anchor.</summary>
        public static string AnchorData(int electionId, int blockCount, string? headHash, DateTime timestamp)
        {
            return string.Join('|', "anchor-v1", electionId, blockCount, headHash ?? string.Empty,
                timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}

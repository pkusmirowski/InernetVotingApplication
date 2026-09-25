using InternetVotingApplication.Models;
using System.Globalization;
using System.Security.Cryptography;

namespace InternetVotingApplication.Blockchain
{
    /// <summary>
    /// Canonical serialization and hashing of a single vote block.
    /// </summary>
    public static class BlockHelper
    {
        /// <summary>Timestamp format without time-zone designator so the value round-trips through the database unchanged.</summary>
        private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff";

        /// <summary>
        /// Builds the string that is hashed for a block. Fields are separated so that
        /// (candidate 1, election 12) and (candidate 11, election 2) can never collide.
        /// </summary>
        public static string VoteData(int index, int candidateId, int electionId, DateTime timestamp, string nonce, string? previousBlockHash)
        {
            ArgumentNullException.ThrowIfNull(nonce);
            var ts = timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture);
            return string.Join('|', "v2", index, candidateId, electionId, ts, nonce, previousBlockHash ?? string.Empty);
        }

        public static string ComputeHash(GlosowanieWyborcze block, string? previousBlockHash)
        {
            ArgumentNullException.ThrowIfNull(block);
            return HashHelper.Hash(VoteData(block.Indeks, block.IdKandydat, block.IdWybory, block.ZnacznikCzasu, block.Nonce, previousBlockHash));
        }

        /// <summary>Creates a random 128-bit nonce encoded as 32 upper-case hex characters.</summary>
        public static string NewNonce()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        }
    }
}

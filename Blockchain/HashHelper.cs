using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace InternetVotingApplication.Blockchain
{
    public static class HashHelper
    {
        /// <summary>
        /// Computes the SHA-256 hash of the input string.
        /// </summary>
        /// <param name="input">The input string to hash.</param>
        /// <returns>The computed hash as a hexadecimal string.</returns>
        public static string Hash(string input)
        {
            return HashInternal(input);
        }

        private static string HashInternal(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return GetStringFromHash(hashBytes);
        }

        private static string GetStringFromHash(IEnumerable<byte> hashBytes)
        {
            var result = new StringBuilder();
            foreach (var b in hashBytes)
            {
                result.Append(b.ToString("X2"));
            }
            return result.ToString();
        }
    }
}
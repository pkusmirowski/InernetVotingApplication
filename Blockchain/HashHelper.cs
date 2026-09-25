using System.Security.Cryptography;
using System.Text;

namespace InternetVotingApplication.Blockchain
{
    public static class HashHelper
    {
        /// <summary>
        /// Computes the SHA-256 hash of the input string and returns it as upper-case hexadecimal (64 characters).
        /// </summary>
        public static string Hash(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        }
    }
}

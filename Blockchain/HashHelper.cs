using System.Security.Cryptography;
using System.Text;

namespace InernetVotingApplication.Blockchain
{
    public static class HashHelper
    {
        public static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha.ComputeHash(inputBytes);
            var result = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                result.Append(b.ToString("x2"));
            }
            return result.ToString();
        }
    }
}

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace InternetVotingApplication.Blockchain
{
    public static class HashHelper
    {
        public static string Hash(string input)
        {
            return HashInternal(input);
        }

        private static string HashInternal(string str)
        {
            using (var sha = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(str);
                byte[] hashBytes = sha.ComputeHash(inputBytes);
                return GetStringFromHash(hashBytes);
            }
        }


        private static string GetStringFromHash(IEnumerable<byte> hash)
        {
            var result = new StringBuilder();
            foreach (var value in hash)
            {
                result.Append(value.ToString("X2"));
            }

            return result.ToString();
        }
    }
}

using System;
using System.Text;

namespace InternetVotingApplication.ExtensionMethods
{
    public static class GeneratePassword
    {
        private const string ValidChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
        private static readonly Random Random = new();

        /// <summary>
        /// Generates a random password with the specified length.
        /// </summary>
        /// <param name="length">The length of the password to generate.</param>
        /// <returns>A randomly generated password.</returns>
        public static string CreateRandomPassword(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentException("Password length must be greater than zero.", nameof(length));
            }

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(ValidChars[Random.Next(ValidChars.Length)]);
            }
            return sb.ToString();
        }
    }
}
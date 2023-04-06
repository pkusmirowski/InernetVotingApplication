using System;
using System.Security.Cryptography;

namespace InernetVotingApplication.ExtensionMethods
{
    public static class GeneratePassword
    {
        private static readonly char[] validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-".ToCharArray();
        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

        public static ReadOnlySpan<char> CreateRandomPassword(int length)
        {
            Span<byte> bytes = stackalloc byte[length];
            rng.GetBytes(bytes);

            Span<char> chars = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                int index = bytes[i] % validChars.Length;
                chars[i] = validChars[index];
            }

            return chars.ToString();
        }
    }
}

using System;
using System.Text;

namespace InternetVotingApplication.ExtensionMethods
{
    public static class GeneratePassword
    {
        private static readonly string ValidChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
        private static readonly Random Random = new();

        public static string CreateRandomPassword(int length)
        {
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(ValidChars[Random.Next(0, ValidChars.Length)]);
            }
            return sb.ToString();
        }
    }
}

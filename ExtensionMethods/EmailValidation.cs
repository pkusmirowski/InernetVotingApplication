using System;
using System.Net.Mail;

namespace InternetVotingApplication.ExtensionMethods
{
    public static class EmailValidation
    {
        /// <summary>
        /// Validates if the provided email address is in a correct format.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if the email address is valid; otherwise, false.</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var addr = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
namespace InternetVotingApplication.Configuration
{
    /// <summary>
    /// Account security settings bound from the <c>Security</c> section.
    /// </summary>
    public class SecurityOptions
    {
        public const string SectionName = "Security";

        /// <summary>Failed sign-in attempts before the account is temporarily locked.</summary>
        public int MaxFailedLoginAttempts { get; set; } = 5;

        /// <summary>How long an account stays locked after too many failed attempts.</summary>
        public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>Validity of a password-reset link.</summary>
        public TimeSpan PasswordResetTokenLifetime { get; set; } = TimeSpan.FromHours(1);
    }
}

namespace InternetVotingApplication.Configuration
{
    /// <summary>
    /// Outbox dispatcher settings bound from the <c>Mail</c> section.
    /// </summary>
    public class MailOptions
    {
        public const string SectionName = "Mail";

        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

        public int MaxAttempts { get; set; } = 5;

        public int BatchSize { get; set; } = 20;
    }
}

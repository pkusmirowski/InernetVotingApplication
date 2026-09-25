namespace InternetVotingApplication.Configuration
{
    /// <summary>
    /// Start-up seeding settings bound from the <c>Seeding</c> section.
    /// </summary>
    public class SeedingOptions
    {
        public const string SectionName = "Seeding";

        /// <summary>
        /// E-mail addresses of registered and activated accounts that should be promoted
        /// to administrators when the application starts.
        /// </summary>
        public IList<string> AdminEmails { get; set; } = [];
    }
}

namespace InternetVotingApplication.Configuration
{
    /// <summary>
    /// Hash-chain maintenance settings bound from the <c>Chain</c> section.
    /// </summary>
    public class ChainOptions
    {
        public const string SectionName = "Chain";

        /// <summary>How often the background worker re-verifies every active chain.</summary>
        public TimeSpan VerificationInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Publish an anchor every this many blocks (0 disables periodic anchors).</summary>
        public int AnchorEveryBlocks { get; set; } = 100;

        /// <summary>E-mail addresses (election committee, observers) that receive anchors.</summary>
        public IList<string> AnchorRecipients { get; set; } = [];

        /// <summary>Keep verifying ended elections for this long after their end date.</summary>
        public TimeSpan VerifyEndedElectionsFor { get; set; } = TimeSpan.FromDays(30);
    }
}

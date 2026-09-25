namespace InternetVotingApplication.Configuration
{
    /// <summary>
    /// Database start-up behaviour bound from the <c>Database</c> section.
    /// </summary>
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        /// <summary>Apply pending EF Core migrations when the application starts.</summary>
        public bool ApplyMigrationsOnStartup { get; set; }

        /// <summary>Create the schema without migrations (integration tests on SQLite).</summary>
        public bool EnsureCreatedOnStartup { get; set; }
    }
}

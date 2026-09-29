namespace InternetVotingApplication.Configuration;

/// <summary>Start-up seeding settings bound from the <c>Seeding</c> section.</summary>
public sealed class SeedingOptions
{
    public const string SectionName = "Seeding";

    /// <summary>
    /// E-mail addresses of registered and activated accounts that should be promoted
    /// to administrators when the application starts.
    /// </summary>
    public IList<string> AdminEmails { get; set; } = [];

    /// <summary>Development convenience: the first account that activates becomes an administrator when none exists.</summary>
    public bool FirstActivatedUserIsAdmin { get; set; }

    /// <summary>Development convenience: create sample elections and candidates when the database has none.</summary>
    public bool SampleData { get; set; }

    /// <summary>
    /// Development convenience: create the fixed, activated accounts listed in <see cref="Data.TestAccounts"/>
    /// (one administrator, several voters) when they do not exist yet.
    /// </summary>
    public bool TestAccounts { get; set; }
}

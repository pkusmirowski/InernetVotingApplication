namespace InternetVotingApplication.Configuration;

public enum DatabaseProvider
{
    /// <summary>Primary engine. Schema managed by EF Core migrations in <c>Migrations/</c>.</summary>
    SqlServer,

    /// <summary>Development-only file database. Schema created with <c>EnsureCreated</c>, never migrated.</summary>
    Sqlite,
}

/// <summary>
/// Database selection and start-up behaviour bound from the <c>Database</c> section.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Engine to use. SQL Server unless explicitly switched.</summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    /// <summary>Connection string of the SQLite file; a relative <c>Data Source</c> resolves against the content root.</summary>
    public string SqliteConnectionString { get; set; } = "Data Source=App_Data/voting-dev.db";

    /// <summary>
    /// Development convenience: when SQL Server does not answer the start-up probe, run on SQLite instead of failing.
    /// Ignored outside the Development environment, so production never switches engines silently.
    /// </summary>
    public bool FallbackToSqliteWhenUnavailable { get; set; }

    /// <summary>How long the start-up probe waits for SQL Server.</summary>
    public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Apply pending EF Core migrations when the application starts (SQL Server only).</summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>Create the schema without migrations (integration tests; always used for SQLite).</summary>
    public bool EnsureCreatedOnStartup { get; set; }
}

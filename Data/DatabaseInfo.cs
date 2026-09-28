using InternetVotingApplication.Configuration;

namespace InternetVotingApplication.Data;

public enum DatabaseSelectionReason
{
    /// <summary>The configured provider is in use; no probe was needed.</summary>
    Configured,

    /// <summary>SQL Server answered the start-up probe and is in use.</summary>
    Probed,

    /// <summary>SQL Server was configured but did not answer; the SQLite fallback is in use (Development only).</summary>
    Fallback,
}

/// <summary>
/// Which database engine the running application uses and why. Registered as a singleton so that the layout
/// banner and the diagnostics page can show it.
/// </summary>
/// <param name="ConnectionString">Effective connection string. Never render it; use <see cref="MaskedConnectionString"/>.</param>
public sealed record DatabaseInfo(
    DatabaseProvider Provider,
    DatabaseSelectionReason Reason,
    string ConnectionString,
    string MaskedConnectionString,
    ProbeResult? Probe,
    string? SqliteFilePath,
    string? Explanation)
{
    public bool IsSqlite => Provider == DatabaseProvider.Sqlite;

    public bool IsFallback => Reason == DatabaseSelectionReason.Fallback;
}

/// <summary>Thrown when the database cannot be used and no fallback is allowed.</summary>
public sealed class DatabaseUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

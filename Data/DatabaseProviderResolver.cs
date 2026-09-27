using System.Text.RegularExpressions;
using InternetVotingApplication.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Data;

/// <summary>
/// Decides which database engine the application runs on. Registered as the factory of the
/// <see cref="DatabaseInfo"/> singleton, so the decision (and the optional probe) happens exactly once,
/// on first use, with logging available.
/// </summary>
public sealed partial class DatabaseProviderResolver(
    IOptions<DatabaseOptions> options,
    IConfiguration configuration,
    IHostEnvironment environment,
    ISqlServerProbe probe,
    ILogger<DatabaseProviderResolver> logger)
{
    public const string ConnectionStringName = "InternetVotingDBConnection";

    /// <summary>Setting this environment variable to 1 skips the SQL Server probe (scripted runs, CI).</summary>
    public const string SkipProbeVariable = "IVAPP_SKIP_DB_PROBE";

    public DatabaseInfo Resolve()
    {
        var info = Resolve(options.Value, configuration.GetConnectionString(ConnectionStringName), environment, probe, EF.IsDesignTime || SkipRequestedByEnvironment());

        switch (info.Reason)
        {
            case DatabaseSelectionReason.Fallback:
                logger.LogWarning("{Explanation}", info.Explanation);
                break;
            case DatabaseSelectionReason.Probed:
                logger.LogInformation("Baza danych: SQL Server ({ConnectionString}), sonda OK w {Elapsed} ms", info.MaskedConnectionString, (int)(info.Probe?.Elapsed.TotalMilliseconds ?? 0));
                break;
            default:
                logger.LogInformation("Baza danych: {Provider} ({ConnectionString})", info.Provider, info.MaskedConnectionString);
                break;
        }

        return info;
    }

    /// <summary>Pure decision, without logging. Public for tests.</summary>
    public static DatabaseInfo Resolve(DatabaseOptions options, string? sqlServerConnectionString, IHostEnvironment environment, ISqlServerProbe probe, bool skipProbe)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(probe);

        if (options.Provider == DatabaseProvider.Sqlite)
        {
            var (connectionString, path) = ResolveSqlite(options.SqliteConnectionString, environment.ContentRootPath);
            return new DatabaseInfo(DatabaseProvider.Sqlite, DatabaseSelectionReason.Configured, connectionString, connectionString, null, path, null);
        }

        if (string.IsNullOrWhiteSpace(sqlServerConnectionString))
        {
            throw new DatabaseUnavailableException(
                $"Brak connection stringa 'ConnectionStrings:{ConnectionStringName}'. Ustaw go w appsettings, user secrets lub zmiennej środowiskowej, " +
                "albo uruchom aplikację w trybie SQLite (Database:Provider=Sqlite).");
        }

        var masked = Mask(sqlServerConnectionString);
        var fallbackPossible = options.FallbackToSqliteWhenUnavailable && environment.IsDevelopment();

        // The probe exists only to decide about the fallback. Without a possible fallback the application behaves
        // exactly as before: SQL Server is used and a failure surfaces (readably) when the schema is initialised.
        if (!fallbackPossible || skipProbe)
        {
            return new DatabaseInfo(DatabaseProvider.SqlServer, DatabaseSelectionReason.Configured, sqlServerConnectionString, masked, null, null, null);
        }

        var result = probe.CanConnect(sqlServerConnectionString, options.ProbeTimeout);
        if (result.Ok)
        {
            return new DatabaseInfo(DatabaseProvider.SqlServer, DatabaseSelectionReason.Probed, sqlServerConnectionString, masked, result, null, null);
        }

        var (sqliteConnectionString, sqlitePath) = ResolveSqlite(options.SqliteConnectionString, environment.ContentRootPath);
        var explanation =
            Explain(sqlServerConnectionString, result.Error, result.ErrorNumber, options.ProbeTimeout) + Environment.NewLine +
            $"TRYB ZAPASOWY: aplikacja działa na SQLite w pliku {sqlitePath ?? "w pamięci"}. Dane z tego pliku nie trafią do SQL Servera. " +
            "Po uruchomieniu SQL Servera zrestartuj aplikację, a wróci na SQL Server.";

        return new DatabaseInfo(DatabaseProvider.Sqlite, DatabaseSelectionReason.Fallback, sqliteConnectionString, sqliteConnectionString, result, sqlitePath, explanation);
    }

    public static bool SkipRequestedByEnvironment()
    {
        var value = Environment.GetEnvironmentVariable(SkipProbeVariable);
        return value is "1" or "true" or "True";
    }

    /// <summary>Applies the resolved provider to a DbContext options builder.</summary>
    public static void Configure(DbContextOptionsBuilder builder, DatabaseInfo info)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(info);

        if (info.Provider == DatabaseProvider.Sqlite)
        {
            builder.UseSqlite(info.ConnectionString);
        }
        else
        {
            builder.UseSqlServer(info.ConnectionString);
        }
    }

    /// <summary>Resolves a relative <c>Data Source</c> against the content root and makes sure its directory exists.</summary>
    public static (string ConnectionString, string? FilePath) ResolveSqlite(string sqliteConnectionString, string contentRoot)
    {
        var builder = new SqliteConnectionStringBuilder(sqliteConnectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase) || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return (builder.ConnectionString, null);
        }

        var path = Path.IsPathRooted(dataSource) ? dataSource : Path.GetFullPath(Path.Combine(contentRoot, dataSource));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        builder.DataSource = path;
        return (builder.ConnectionString, path);
    }

    /// <summary>Hides the password of a connection string so it can be shown on a page or in a log.</summary>
    public static string Mask(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        return PasswordRegex().Replace(connectionString, "$1=***");
    }

    /// <summary>Human-readable, Polish explanation of a SQL Server connection failure with the most likely fixes.</summary>
    public static string Explain(string connectionString, string? error, int? errorNumber, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        var server = "?";
        var database = "?";
        var windowsAuth = false;
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            server = string.IsNullOrEmpty(builder.DataSource) ? "(brak)" : builder.DataSource;
            database = builder.InitialCatalog;
            windowsAuth = builder.IntegratedSecurity;
        }
        catch (ArgumentException)
        {
            // Malformed connection string: the generic advice below still applies.
        }

        var cause = errorNumber switch
        {
            -1 or 2 or 53 or 26 or 40 or 10061 or 233 =>
                $"serwer '{server}' nie nasłuchuje: usługa SQL Server jest zatrzymana (services.msc, pozycja 'SQL Server (MSSQLSERVER)' lub 'SQL Server (NAZWA)'), " +
                "nazwa instancji jest inna (np. localhost\\SQLEXPRESS, (localdb)\\MSSQLLocalDB) albo protokół TCP/IP jest wyłączony w SQL Server Configuration Manager",
            4060 => $"serwer odpowiada, ale baza '{database}' nie istnieje lub konto nie ma do niej dostępu",
            18456 => windowsAuth
                ? "logowanie odrzucone: konto Windows, na którym działa aplikacja, nie ma loginu na tym serwerze"
                : "logowanie odrzucone: niepoprawny login lub hasło",
            _ => "serwer nie odpowiedział lub odrzucił połączenie",
        };

        var timeoutText = timeout.HasValue ? $" w ciągu {timeout.Value.TotalSeconds:0} s" : string.Empty;
        return
            $"Nie udało się połączyć z SQL Server '{server}' (baza '{database}', {(windowsAuth ? "uwierzytelnianie Windows" : "login SQL")}){timeoutText}." + Environment.NewLine +
            $"Szczegóły: {error ?? "brak"}{(errorNumber.HasValue ? $" (kod {errorNumber})" : string.Empty)}." + Environment.NewLine +
            $"Najbardziej prawdopodobna przyczyna: {cause}." + Environment.NewLine +
            "Jak sprawdzić: w konsoli 'sqlcmd -S " + server + " -E -Q \"SELECT @@VERSION\"'." + Environment.NewLine +
            "Jak zmienić połączenie: dotnet user-secrets set \"ConnectionStrings:" + ConnectionStringName + "\" \"Server=...;Database=InternetVoting;Trusted_Connection=True;TrustServerCertificate=True;\"." + Environment.NewLine +
            "Jak pracować bez SQL Servera: profil startowy 'https (SQLite)' albo zmienna środowiskowa Database__Provider=Sqlite.";
    }

    [GeneratedRegex(@"(?i)\b(Password|Pwd)\s*=\s*[^;]*")]
    private static partial Regex PasswordRegex();
}

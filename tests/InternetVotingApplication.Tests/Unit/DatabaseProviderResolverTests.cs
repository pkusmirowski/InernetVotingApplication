using InternetVotingApplication.Configuration;
using InternetVotingApplication.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace InternetVotingApplication.Tests.Unit;

public sealed class DatabaseProviderResolverTests : IDisposable
{
    private const string SqlServerConnection = "Server=localhost;Database=InternetVoting;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "ivapp-tests", Guid.NewGuid().ToString("N"));

    private IHostEnvironment Environment(string name)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(name);
        env.ContentRootPath.Returns(_root);
        return env;
    }

    private static DatabaseOptions Options(DatabaseProvider provider = DatabaseProvider.SqlServer, bool fallback = true) => new()
    {
        Provider = provider,
        FallbackToSqliteWhenUnavailable = fallback,
        ProbeTimeout = TimeSpan.FromSeconds(2),
        SqliteConnectionString = "Data Source=App_Data/test.db",
    };

    [Fact]
    public void Explicit_sqlite_never_probes_and_resolves_file_under_content_root()
    {
        var probe = new FakeSqlServerProbe(ok: false);

        var info = DatabaseProviderResolver.Resolve(Options(DatabaseProvider.Sqlite), null, Environment("Production"), probe, skipProbe: false);

        Assert.Equal(DatabaseProvider.Sqlite, info.Provider);
        Assert.Equal(DatabaseSelectionReason.Configured, info.Reason);
        Assert.Equal(0, probe.Calls);
        Assert.StartsWith(_root, info.SqliteFilePath, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.GetDirectoryName(info.SqliteFilePath!)));
        Assert.Contains(info.SqliteFilePath!, info.ConnectionString, StringComparison.Ordinal);
    }

    [Fact]
    public void Sql_server_with_working_probe_is_used()
    {
        var probe = new FakeSqlServerProbe(ok: true);

        var info = DatabaseProviderResolver.Resolve(Options(), SqlServerConnection, Environment("Development"), probe, skipProbe: false);

        Assert.Equal(DatabaseProvider.SqlServer, info.Provider);
        Assert.Equal(DatabaseSelectionReason.Probed, info.Reason);
        Assert.Equal(1, probe.Calls);
        Assert.Equal(TimeSpan.FromSeconds(2), probe.LastTimeout);
        Assert.Equal(SqlServerConnection, info.ConnectionString);
        Assert.Null(info.Explanation);
    }

    [Fact]
    public void Unreachable_sql_server_in_development_falls_back_to_sqlite_with_explanation()
    {
        var probe = new FakeSqlServerProbe(ok: false, "A network-related error", 2);

        var info = DatabaseProviderResolver.Resolve(Options(), SqlServerConnection, Environment("Development"), probe, skipProbe: false);

        Assert.Equal(DatabaseProvider.Sqlite, info.Provider);
        Assert.True(info.IsFallback);
        Assert.NotNull(info.SqliteFilePath);
        Assert.NotNull(info.Explanation);
        Assert.Contains("localhost", info.Explanation, StringComparison.Ordinal);
        Assert.Contains("TRYB ZAPASOWY", info.Explanation, StringComparison.Ordinal);
        Assert.Contains("services.msc", info.Explanation, StringComparison.Ordinal);
        Assert.Contains("SQLite", info.Explanation, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Outside_development_sql_server_is_used_without_probing(string environment)
    {
        var probe = new FakeSqlServerProbe(ok: false);

        var info = DatabaseProviderResolver.Resolve(Options(fallback: true), SqlServerConnection, Environment(environment), probe, skipProbe: false);

        Assert.Equal(DatabaseProvider.SqlServer, info.Provider);
        Assert.Equal(DatabaseSelectionReason.Configured, info.Reason);
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public void Fallback_disabled_means_no_probe_even_in_development()
    {
        var probe = new FakeSqlServerProbe(ok: false);

        var info = DatabaseProviderResolver.Resolve(Options(fallback: false), SqlServerConnection, Environment("Development"), probe, skipProbe: false);

        Assert.Equal(DatabaseProvider.SqlServer, info.Provider);
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public void Skip_flag_disables_the_probe()
    {
        var probe = new FakeSqlServerProbe(ok: false);

        var info = DatabaseProviderResolver.Resolve(Options(), SqlServerConnection, Environment("Development"), probe, skipProbe: true);

        Assert.Equal(DatabaseProvider.SqlServer, info.Provider);
        Assert.Equal(DatabaseSelectionReason.Configured, info.Reason);
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public void Missing_connection_string_throws_readable_error()
    {
        var ex = Assert.Throws<DatabaseUnavailableException>(() =>
            DatabaseProviderResolver.Resolve(Options(), "  ", Environment("Development"), new FakeSqlServerProbe(true), skipProbe: false));

        Assert.Contains("InternetVotingDBConnection", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Mask_hides_passwords_only()
    {
        Assert.Equal("Server=x;User Id=sa;Password=***;", DatabaseProviderResolver.Mask("Server=x;User Id=sa;Password=Voting!Passw0rd;"));
        Assert.Equal("Server=x;pwd=***", DatabaseProviderResolver.Mask("Server=x;pwd=abc"));
        Assert.Equal(SqlServerConnection, DatabaseProviderResolver.Mask(SqlServerConnection));
    }

    [Theory]
    [InlineData(2, "zatrzymana")]
    [InlineData(53, "zatrzymana")]
    [InlineData(4060, "nie istnieje")]
    [InlineData(18456, "logowanie odrzucone")]
    [InlineData(null, "nie odpowiedział")]
    public void Explain_maps_error_numbers_to_polish_causes(int? number, string fragment)
    {
        var text = DatabaseProviderResolver.Explain(SqlServerConnection, "opis", number, TimeSpan.FromSeconds(3));

        Assert.Contains(fragment, text, StringComparison.Ordinal);
        Assert.Contains("localhost", text, StringComparison.Ordinal);
        Assert.Contains("https (SQLite)", text, StringComparison.Ordinal);
        Assert.Contains("user-secrets", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_applies_matching_ef_provider()
    {
        var sqlServer = new DbContextOptionsBuilder();
        DatabaseProviderResolver.Configure(sqlServer, new DatabaseInfo(DatabaseProvider.SqlServer, DatabaseSelectionReason.Configured, SqlServerConnection, SqlServerConnection, null, null, null));
        Assert.Contains(sqlServer.Options.Extensions, e => e.GetType().Name == "SqlServerOptionsExtension");

        var sqlite = new DbContextOptionsBuilder();
        DatabaseProviderResolver.Configure(sqlite, new DatabaseInfo(DatabaseProvider.Sqlite, DatabaseSelectionReason.Configured, "Data Source=:memory:", "Data Source=:memory:", null, null, null));
        Assert.Contains(sqlite.Options.Extensions, e => e.GetType().Name == "SqliteOptionsExtension");
    }

    [Fact]
    public void ResolveSqlite_leaves_memory_and_absolute_paths_alone()
    {
        Assert.Null(DatabaseProviderResolver.ResolveSqlite("Data Source=:memory:", _root).FilePath);

        var absolute = Path.Combine(_root, "abs", "x.db");
        var (cs, path) = DatabaseProviderResolver.ResolveSqlite($"Data Source={absolute}", "/elsewhere");
        Assert.Equal(absolute, path);
        Assert.Contains(absolute, cs, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

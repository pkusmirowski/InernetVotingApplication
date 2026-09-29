using InternetVotingApplication.Configuration;
using InternetVotingApplication.Data;
using InternetVotingApplication.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace InternetVotingApplication.Tests.Services;

/// <summary><see cref="DbInitializer.InitializeAsync"/> as the application runs it at start-up.</summary>
public sealed class DbInitializerStartupTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ivapp-tests", Guid.NewGuid().ToString("N"));

    public DbInitializerStartupTests()
    {
        Directory.CreateDirectory(_root);
    }

    private static ServiceProvider Services(DatabaseInfo info, SeedingOptions seeding, string environmentName = "Development")
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        var services = new ServiceCollection();
        services.AddSingleton(info);
        services.AddDbContext<InternetVotingContext>(options => DatabaseProviderResolver.Configure(options, info));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<TimeProvider>(TestData.Clock());
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(seeding));
        services.AddSingleton(environment);
        return services.BuildServiceProvider();
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
    }

    [Fact]
    public async Task Sqlite_start_creates_the_schema_sample_data_and_test_accounts()
    {
        var file = Path.Combine(_root, "start.db");
        var info = new DatabaseInfo(DatabaseProvider.Sqlite, DatabaseSelectionReason.Configured, $"Data Source={file}", $"Data Source={file}", null, file, null);
        using var services = Services(info, new SeedingOptions { SampleData = true, TestAccounts = true });

        await DbInitializer.InitializeAsync(services, Configuration());

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InternetVotingContext>();
        Assert.NotEmpty(context.DataWyborows);
        Assert.Equal(TestAccounts.All.Count, context.Uzytkowniks.Count(u => u.Email.EndsWith("@test.local")));
        Assert.Single(context.Administrators);
    }

    [Fact]
    public async Task Unreachable_sql_server_stops_the_start_with_an_explanation()
    {
        const string connection = "Server=127.0.0.1,1;Database=InternetVoting;User Id=sa;Password=x;Connect Timeout=1;TrustServerCertificate=True";
        var info = new DatabaseInfo(DatabaseProvider.SqlServer, DatabaseSelectionReason.Configured, connection, connection, null, null, null);
        using var services = Services(info, new SeedingOptions());

        var ex = await Assert.ThrowsAsync<DatabaseUnavailableException>(() =>
            DbInitializer.InitializeAsync(services, Configuration(("Database:ApplyMigrationsOnStartup", "true"))));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task Start_outside_development_without_schema_work_does_not_touch_an_unreachable_server()
    {
        const string connection = "Server=127.0.0.1,1;Database=InternetVoting;User Id=sa;Password=x;Connect Timeout=1;TrustServerCertificate=True";
        var info = new DatabaseInfo(DatabaseProvider.SqlServer, DatabaseSelectionReason.Configured, connection, connection, null, null, null);
        using var services = Services(info, new SeedingOptions(), environmentName: "Production");

        // The test-account check fails on the unreachable server; it is logged, not thrown.
        await DbInitializer.InitializeAsync(services, Configuration());
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

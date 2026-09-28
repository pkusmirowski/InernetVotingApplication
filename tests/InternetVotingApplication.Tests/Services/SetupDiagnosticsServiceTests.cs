using InternetVotingApplication.Configuration;
using InternetVotingApplication.Data;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.Services.Mail;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InternetVotingApplication.Tests.Services;

public sealed class SetupDiagnosticsServiceTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ivapp-tests", Guid.NewGuid().ToString("N"));

    private SetupDiagnosticsService Create(InternetVotingContext context, DatabaseInfo? info = null, ISmtpTransport? transport = null, SeedingOptions? seeding = null)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        environment.ContentRootPath.Returns(_root);
        info ??= new DatabaseInfo(DatabaseProvider.Sqlite, DatabaseSelectionReason.Configured, "Data Source=:memory:", "Data Source=:memory:", null, null, null);
        transport ??= new PickupDirectoryTransport(Options.Create(new SmtpOptions { PickupDirectory = "mail" }), environment, NullLogger<PickupDirectoryTransport>.Instance);
        return new SetupDiagnosticsService(
            context, info, TestData.Signer, transport,
            Options.Create(new DatabaseOptions()),
            Options.Create(new SmtpOptions { PickupDirectory = "mail" }),
            Options.Create(new SigningOptions { KeyFilePath = "App_Data/signing-key.pem", AutoGenerateKey = true }),
            Options.Create(seeding ?? new SeedingOptions { FirstActivatedUserIsAdmin = true, SampleData = true }),
            environment);
    }

    [Fact]
    public async Task Reports_schema_data_admin_and_next_steps()
    {
        using var context = _db.CreateContext();
        var service = Create(context);

        var empty = await service.CollectAsync();
        Assert.Equal(SetupState.Warning, empty.OverallState);
        Assert.Contains(empty.Database, i => i.Label == "Schemat" && i.State == SetupState.Ok);
        Assert.Contains(empty.Database, i => i.Label == "Migracje" && i.Value.Contains("SQLite", StringComparison.Ordinal));
        Assert.Contains(empty.Data, i => i.Label == "Administratorzy" && i.State == SetupState.Warning);
        Assert.Contains(empty.NextSteps, s => s.Contains("Zarejestruj konto", StringComparison.Ordinal));
        Assert.Empty(empty.Errors);

        var admin = TestData.User();
        context.Uzytkowniks.Add(admin);
        context.Administrators.Add(new Administrator { IdUzytkownikNavigation = admin });
        context.DataWyborows.Add(TestData.OngoingElection());
        await context.SaveChangesAsync();

        var filled = await service.CollectAsync();
        Assert.Contains(filled.Data, i => i.Label == "Administratorzy" && i.State == SetupState.Ok);
        Assert.Contains(filled.Data, i => i.Label == "Wybory / kandydaci" && i.Value.StartsWith("1 /", StringComparison.Ordinal));
        Assert.DoesNotContain(filled.NextSteps, s => s.Contains("Zarejestruj konto", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Counts_mail_files_and_reports_fallback()
    {
        using var context = _db.CreateContext();
        var mailDir = Path.Combine(_root, "mail");
        Directory.CreateDirectory(mailDir);
        await File.WriteAllTextAsync(Path.Combine(mailDir, "a.html"), "x");
        await File.WriteAllTextAsync(Path.Combine(mailDir, "b.html"), "y");
        var fallback = new DatabaseInfo(DatabaseProvider.Sqlite, DatabaseSelectionReason.Fallback, "Data Source=:memory:", "Data Source=:memory:",
            new ProbeResult(false, "down", 2, TimeSpan.Zero), Path.Combine(_root, "App_Data", "x.db"), "SQL Server nie odpowiedział");

        var vm = await Create(context, fallback).CollectAsync();

        Assert.Contains(vm.Mail, i => i.Label == "Wiadomości" && i.Value == "2");
        Assert.Contains(vm.Mail, i => i.Label == "Katalog" && i.Value == mailDir);
        Assert.Contains(vm.Database, i => i.Label == "Silnik" && i.State == SetupState.Warning);
        Assert.Contains(vm.Database, i => i.Label == "Plik SQLite" && i.State == SetupState.Warning);
        Assert.Equal("SQL Server nie odpowiedział", vm.FallbackExplanation);
        Assert.Contains(vm.NextSteps, s => s.Contains("services.msc", StringComparison.Ordinal));
        Assert.Contains(vm.Signing, i => i.Label == "Identyfikator klucza" && i.Value == TestData.Signer.KeyId);
    }

    [Fact]
    public async Task Survives_a_broken_database()
    {
        var options = new DbContextOptionsBuilder<InternetVotingContext>()
            .UseSqlite("Data Source=" + Path.Combine(_root, "missing", "nested", "db.sqlite") + ";Mode=ReadOnly")
            .Options;
        using var broken = new InternetVotingContext(options);

        var vm = await Create(broken).CollectAsync();

        Assert.Equal(SetupState.Error, vm.OverallState);
        Assert.NotEmpty(vm.Errors);
        Assert.NotEmpty(vm.NextSteps);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

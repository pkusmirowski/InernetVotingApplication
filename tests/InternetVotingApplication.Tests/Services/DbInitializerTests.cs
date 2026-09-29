using InternetVotingApplication.Data;
using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InternetVotingApplication.Tests.Services;

public sealed class DbInitializerTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    [Fact]
    public async Task Promotes_configured_accounts_once_and_ignores_unknown_addresses()
    {
        using var context = _db.CreateContext();
        var user = TestData.User(email: "admin@example.com");
        context.Uzytkowniks.Add(user);
        await context.SaveChangesAsync();

        await DbInitializer.PromoteAdministratorsAsync(context, [" Admin@Example.com ", "nobody@example.com", ""], TestData.Clock(), NullLogger.Instance);
        await DbInitializer.PromoteAdministratorsAsync(context, ["admin@example.com"], TestData.Clock(), NullLogger.Instance);

        var admin = Assert.Single(context.Administrators);
        Assert.Equal(user.Id, admin.IdUzytkownik);
        Assert.Single(context.DziennikAudytu, a => a.Akcja == "AdminPromoted");
    }

    [Fact]
    public async Task Sample_data_is_created_only_into_an_empty_database()
    {
        using var context = _db.CreateContext();

        Assert.True(await DbInitializer.SeedSampleDataAsync(context, TestData.Clock(), NullLogger.Instance));
        Assert.False(await DbInitializer.SeedSampleDataAsync(context, TestData.Clock(), NullLogger.Instance));

        var elections = await context.DataWyborows.Include(e => e.Kandydats).ToListAsync();
        Assert.Equal(3, elections.Count);
        Assert.Contains(elections, e => e.GetStatus(TestData.Now) == ElectionStatus.Ongoing && e.Kandydats.Count == 3);
        Assert.Contains(elections, e => e.GetStatus(TestData.Now) == ElectionStatus.Upcoming);
        Assert.Contains(elections, e => e.GetStatus(TestData.Now) == ElectionStatus.Ended);
    }

    [Fact]
    public async Task Test_accounts_are_disabled_outside_development()
    {
        using var context = _db.CreateContext();
        await DbInitializer.SeedTestAccountsAsync(context, TestData.Clock(), NullLogger.Instance);
        var real = TestData.User("prawdziwy@example.com", "90010112349");
        context.Uzytkowniks.Add(real);
        await context.SaveChangesAsync();

        Assert.Equal(TestAccounts.All.Count, await DbInitializer.DisableTestAccountsAsync(context, TestData.Clock(), NullLogger.Instance));
        Assert.Equal(0, await DbInitializer.DisableTestAccountsAsync(context, TestData.Clock(), NullLogger.Instance));

        using var check = _db.CreateContext();
        Assert.All(check.Uzytkowniks.Where(u => u.Email.EndsWith("@test.local")), u => Assert.False(u.JestAktywne));
        Assert.Empty(check.Administrators);
        Assert.True(check.Uzytkowniks.Single(u => u.Email == "prawdziwy@example.com").JestAktywne);
        Assert.Single(check.DziennikAudytu, a => a.Akcja == "TestAccountsDisabled");
    }

    [Fact]
    public async Task Test_accounts_are_created_once_activated_and_with_one_administrator()
    {
        using var context = _db.CreateContext();

        Assert.Equal(TestAccounts.All.Count, await DbInitializer.SeedTestAccountsAsync(context, TestData.Clock(), NullLogger.Instance));
        Assert.Equal(0, await DbInitializer.SeedTestAccountsAsync(context, TestData.Clock(), NullLogger.Instance));

        var users = await context.Uzytkowniks.Include(u => u.Administrators).ToListAsync();
        Assert.Equal(TestAccounts.All.Count, users.Count);
        Assert.All(users, u => Assert.True(u.JestAktywne));
        Assert.All(users, u => Assert.True(InternetVotingApplication.ExtensionMethods.PeselValidation.IsValidPESEL(u.Pesel)));
        var admin = Assert.Single(users, u => u.Administrators.Count > 0);
        Assert.Equal("admin@test.local", admin.Email);
        Assert.True(BCrypt.Net.BCrypt.Verify(TestAccounts.AdminPassword, admin.Haslo));
        Assert.True(BCrypt.Net.BCrypt.Verify(TestAccounts.VoterPassword, users.First(u => u.Email == "wyborca1@test.local").Haslo));
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

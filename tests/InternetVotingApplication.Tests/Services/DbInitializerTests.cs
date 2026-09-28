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

    public void Dispose()
    {
        _db.Dispose();
    }
}

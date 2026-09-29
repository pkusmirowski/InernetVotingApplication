using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InternetVotingApplication.Tests.Services;

public sealed class AdminServiceTests : IDisposable
{
    private readonly SqliteDatabase _db = new();

    private static AdminService CreateService(InternetVotingContext context) => new(context, TestData.Audit(context, TestData.Clock()), TestData.Clock(), TestData.Logger<AdminService>());

    [Fact]
    public async Task Add_election_validates_dates_and_uniqueness()
    {
        using var context = _db.CreateContext();
        var service = CreateService(context);
        var model = new ElectionFormViewModel { Opis = " Wybory 2026 ", DataRozpoczecia = TestData.Now, DataZakonczenia = TestData.Now.AddDays(1) };

        Assert.Equal(AddElectionStatus.Success, await service.AddElectionAsync(model, actorUserId: 7));
        Assert.Equal("Wybory 2026", context.DataWyborows.Single().Opis);
        var audit = Assert.Single(context.DziennikAudytu);
        Assert.Equal("ElectionCreated", audit.Akcja);
        Assert.Equal(7, audit.IdUzytkownik);
        Assert.Equal(AddElectionStatus.Duplicate, await service.AddElectionAsync(model));
        Assert.Equal(AddElectionStatus.InvalidDates, await service.AddElectionAsync(new ElectionFormViewModel { Opis = "X", DataRozpoczecia = TestData.Now, DataZakonczenia = TestData.Now }));
    }

    [Fact]
    public async Task Add_candidate_requires_existing_election_and_unique_name_per_election()
    {
        using var context = _db.CreateContext();
        var first = TestData.OngoingElection("A");
        var second = TestData.OngoingElection("B");
        context.AddRange(first, second);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        Assert.Equal(AddCandidateStatus.Success, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = first.Id }));
        Assert.Equal(AddCandidateStatus.Duplicate, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = " Jan ", Nazwisko = "Nowak", IdWybory = first.Id }));
        Assert.Equal(AddCandidateStatus.Success, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = second.Id }));
        Assert.Equal(AddCandidateStatus.ElectionNotFound, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = 999 }));
        Assert.Equal(AddCandidateStatus.ElectionNotFound, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = null }));
    }

    [Fact]
    public async Task Delete_candidate_refuses_when_votes_exist()
    {
        using var context = _db.CreateContext();
        var election = TestData.OngoingElection();
        var voted = new Kandydat { Imie = "A", Nazwisko = "A", IdWyboryNavigation = election };
        var fresh = new Kandydat { Imie = "B", Nazwisko = "B", IdWyboryNavigation = election };
        context.AddRange(election, voted, fresh);
        context.GlosowanieWyborczes.Add(new GlosowanieWyborcze { IdKandydatNavigation = voted, IdWyboryNavigation = election, Indeks = 0, Nonce = new string('0', 32), Hash = new string('0', 64), Podpis = "x", IdKlucza = "k", ZnacznikCzasu = TestData.Now });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        Assert.Equal(DeleteCandidateStatus.HasVotes, await service.DeleteCandidateAsync(voted.Id));
        Assert.Equal(DeleteCandidateStatus.Success, await service.DeleteCandidateAsync(fresh.Id));
        Assert.Equal(DeleteCandidateStatus.NotFound, await service.DeleteCandidateAsync(fresh.Id));

        var list = await service.GetCandidatesAsync(election.Id);
        var item = Assert.Single(list.Candidates);
        Assert.Equal(voted.Id, item.Id);
        Assert.Equal(1, item.VoteCount);
    }

    [Fact]
    public async Task Update_election_changes_name_and_dates_and_rejects_duplicates()
    {
        using var context = _db.CreateContext();
        var first = TestData.OngoingElection("A");
        var second = TestData.OngoingElection("B");
        context.AddRange(first, second);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var model = new ElectionFormViewModel { Opis = " C ", DataRozpoczecia = TestData.Now.AddDays(-5), DataZakonczenia = TestData.Now.AddDays(-4) };
        Assert.Equal(UpdateElectionStatus.Success, await service.UpdateElectionAsync(first.Id, model, actorUserId: 7));

        var updated = await context.DataWyborows.AsNoTracking().SingleAsync(e => e.Id == first.Id);
        Assert.Equal("C", updated.Opis);
        Assert.Equal(ElectionStatus.Ended, updated.GetStatus(TestData.Now));
        Assert.Equal(1, updated.Wersja);
        Assert.Single(context.DziennikAudytu, a => a.Akcja == "ElectionUpdated" && a.IdUzytkownik == 7);

        Assert.Equal(UpdateElectionStatus.Duplicate, await service.UpdateElectionAsync(first.Id, new ElectionFormViewModel { Opis = "B", DataRozpoczecia = TestData.Now, DataZakonczenia = TestData.Now.AddDays(1) }));
        Assert.Equal(UpdateElectionStatus.InvalidDates, await service.UpdateElectionAsync(first.Id, new ElectionFormViewModel { Opis = "X", DataRozpoczecia = TestData.Now, DataZakonczenia = TestData.Now }));
        Assert.Equal(UpdateElectionStatus.NotFound, await service.UpdateElectionAsync(999, model));
        Assert.Null(await service.GetElectionAsync(999));
        Assert.Equal("B", (await service.GetElectionAsync(second.Id))!.Opis);
    }

    [Fact]
    public async Task Delete_election_removes_dependents_but_refuses_when_votes_exist()
    {
        using var context = _db.CreateContext();
        var empty = TestData.OngoingElection("Empty");
        var voted = TestData.OngoingElection("Voted");
        var candidate = new Kandydat { Imie = "A", Nazwisko = "A", IdWyboryNavigation = voted };
        context.AddRange(empty, voted, candidate,
            new Kandydat { Imie = "B", Nazwisko = "B", IdWyboryNavigation = empty },
            new KotwicaLancucha { IdWyboryNavigation = empty, Data = TestData.Now, Powod = "Manual", Podpis = "x" },
            new WeryfikacjaLancucha { IdWyboryNavigation = empty, Data = TestData.Now, Poprawny = true, Wyzwalacz = "Manual" });
        context.GlosowanieWyborczes.Add(new GlosowanieWyborcze { IdKandydatNavigation = candidate, IdWyboryNavigation = voted, Indeks = 0, Nonce = new string('0', 32), Hash = new string('0', 64), Podpis = "x", IdKlucza = "k", ZnacznikCzasu = TestData.Now });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        Assert.Equal(DeleteElectionStatus.HasVotes, await service.DeleteElectionAsync(voted.Id));
        Assert.Equal(DeleteElectionStatus.Success, await service.DeleteElectionAsync(empty.Id, actorUserId: 7));
        Assert.Equal(DeleteElectionStatus.NotFound, await service.DeleteElectionAsync(empty.Id));

        Assert.Single(context.DataWyborows);
        Assert.Single(context.Kandydats);
        Assert.Empty(context.Kotwice);
        Assert.Empty(context.Weryfikacje);
        Assert.Single(context.DziennikAudytu, a => a.Akcja == "ElectionDeleted");
    }

    [Fact]
    public async Task Users_can_be_activated_and_given_or_stripped_of_the_admin_role()
    {
        using var context = _db.CreateContext();
        var admin = TestData.User("admin@example.com", "44051401359");
        var inactive = TestData.User("new@example.com", "90010112349", active: false);
        context.AddRange(admin, inactive);
        context.Administrators.Add(new Administrator { IdUzytkownikNavigation = admin });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        Assert.Equal(UserActionStatus.Success, await service.ActivateUserAsync(inactive.Id, admin.Id));
        Assert.Equal(UserActionStatus.NoChange, await service.ActivateUserAsync(inactive.Id, admin.Id));
        Assert.Equal(UserActionStatus.NotFound, await service.ActivateUserAsync(999, admin.Id));
        var activated = await context.Uzytkowniks.AsNoTracking().SingleAsync(u => u.Id == inactive.Id);
        Assert.True(activated.JestAktywne);
        Assert.Null(activated.KodAktywacyjny);

        // The only administrator cannot be removed, nor can an administrator remove themselves.
        Assert.Equal(UserActionStatus.Forbidden, await service.SetAdministratorAsync(admin.Id, false, admin.Id));
        Assert.Equal(UserActionStatus.Success, await service.SetAdministratorAsync(inactive.Id, true, admin.Id));
        Assert.Equal(UserActionStatus.NoChange, await service.SetAdministratorAsync(inactive.Id, true, admin.Id));
        Assert.Equal(UserActionStatus.Forbidden, await service.SetAdministratorAsync(admin.Id, false, admin.Id));
        Assert.Equal(UserActionStatus.Success, await service.SetAdministratorAsync(admin.Id, false, inactive.Id));
        Assert.Equal(UserActionStatus.NotFound, await service.SetAdministratorAsync(999, true, inactive.Id));

        var users = await service.GetUsersAsync();
        Assert.Equal(2, users.Count);
        Assert.False(users.Single(u => u.Id == admin.Id).IsAdmin);
        Assert.True(users.Single(u => u.Id == inactive.Id).IsAdmin);
        Assert.Contains(context.DziennikAudytu, a => a.Akcja == "UserActivated");
        Assert.Contains(context.DziennikAudytu, a => a.Akcja == "AdminPromoted");
        Assert.Contains(context.DziennikAudytu, a => a.Akcja == "AdminRevoked");
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

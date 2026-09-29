using System.Data.Common;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace InternetVotingApplication.Tests.Services;

/// <summary>
/// Error paths of the services: races on unique indexes, concurrency tokens and foreign keys (injected with
/// <see cref="FailingSaveInterceptor"/>), invalid input and state that only tampering can produce.
/// </summary>
public sealed class FailurePathTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly FakeEmailSender _email = new();
    private readonly FakeTimeProvider _clock = TestData.Clock();

    private AdminService Admin(InternetVotingContext context) => new(context, TestData.Audit(context, _clock), _clock, TestData.Logger<AdminService>());

    private UserService Users(InternetVotingContext context) => new(
        context,
        _email,
        Options.Create(new SecurityOptions()),
        Options.Create(new SeedingOptions()),
        TestData.Audit(context, _clock),
        _clock,
        TestData.Logger<UserService>());

    private static DataWyborow Upcoming(string name = "Przyszłe") => new() { Opis = name, DataRozpoczecia = TestData.Now.AddDays(1), DataZakonczenia = TestData.Now.AddDays(2) };

    private static GlosowanieWyborcze Block(Kandydat candidate, DataWyborow election, int index = 0) => new()
    {
        IdKandydatNavigation = candidate,
        IdWyboryNavigation = election,
        Indeks = index,
        Nonce = new string('0', 32),
        Hash = new string('0', 64),
        Podpis = "x",
        IdKlucza = "k",
        ZnacznikCzasu = TestData.Now,
    };

    private static DbUpdateException UniqueViolation() => new("UNIQUE constraint failed");

    [Fact]
    public async Task Admin_races_on_unique_indexes_are_reported_as_duplicates()
    {
        var failing = new FailingSaveInterceptor(context => context.ChangeTracker.Entries().Any(e => e.State == EntityState.Added && e.Entity is DataWyborow or Kandydat), UniqueViolation, times: 2);
        using var context = _db.CreateContext(failing);
        var election = Upcoming();
        using (var setup = _db.CreateContext())
        {
            setup.DataWyborows.Add(election);
            await setup.SaveChangesAsync();
        }

        var service = Admin(context);

        Assert.Equal(AddElectionStatus.Duplicate, await service.AddElectionAsync(new ElectionFormViewModel { Opis = "Nowe", DataRozpoczecia = TestData.Now, DataZakonczenia = TestData.Now.AddDays(1) }));
        context.ChangeTracker.Clear();
        Assert.Equal(AddCandidateStatus.Duplicate, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "Jan", Nazwisko = "Nowak", IdWybory = election.Id }));
        Assert.Equal(2, failing.Failures);
    }

    [Fact]
    public async Task Election_edit_that_loses_a_race_reports_conflict_or_duplicate()
    {
        using (var setup = _db.CreateContext())
        {
            setup.DataWyborows.Add(Upcoming());
            await setup.SaveChangesAsync();
        }

        var form = new ElectionFormViewModel { Opis = "Zmienione", DataRozpoczecia = TestData.Now.AddDays(1), DataZakonczenia = TestData.Now.AddDays(3) };

        using (var context = _db.CreateContext(new FailingSaveInterceptor(FailingSaveInterceptor.Pending<DataWyborow>(EntityState.Modified), () => new DbUpdateConcurrencyException("version changed"))))
        {
            Assert.Equal(UpdateElectionStatus.Conflict, await Admin(context).UpdateElectionAsync(1, form));
        }

        using (var context = _db.CreateContext(new FailingSaveInterceptor(FailingSaveInterceptor.Pending<DataWyborow>(EntityState.Modified), UniqueViolation)))
        {
            Assert.Equal(UpdateElectionStatus.Duplicate, await Admin(context).UpdateElectionAsync(1, form));
        }

        using var check = _db.CreateContext();
        Assert.Equal("Przyszłe", check.DataWyborows.Single().Opis);
    }

    [Fact]
    public async Task Deletes_refused_by_a_foreign_key_report_existing_votes()
    {
        var election = Upcoming();
        var candidate = new Kandydat { Imie = "A", Nazwisko = "A", IdWyboryNavigation = election };
        using (var setup = _db.CreateContext())
        {
            setup.AddRange(election, candidate);
            await setup.SaveChangesAsync();
        }

        var foreignKey = () => new DbUpdateException("FOREIGN KEY constraint failed");
        using (var context = _db.CreateContext(new FailingSaveInterceptor(FailingSaveInterceptor.Pending<Kandydat>(EntityState.Deleted), foreignKey)))
        {
            Assert.Equal(DeleteCandidateStatus.HasVotes, await Admin(context).DeleteCandidateAsync(candidate.Id));
        }

        using (var context = _db.CreateContext(new FailingSaveInterceptor(FailingSaveInterceptor.Pending<DataWyborow>(EntityState.Deleted), foreignKey)))
        {
            Assert.Equal(DeleteElectionStatus.HasVotes, await Admin(context).DeleteElectionAsync(election.Id));
        }

        using var check = _db.CreateContext();
        Assert.Single(check.Kandydats);
        Assert.Single(check.DataWyborows);
    }

    [Fact]
    public async Task Candidate_with_votes_is_never_deleted()
    {
        // Votes before the start cannot happen through the application; the check guards against tampered data.
        using var context = _db.CreateContext();
        var election = Upcoming();
        var candidate = new Kandydat { Imie = "A", Nazwisko = "A", IdWyboryNavigation = election };
        context.AddRange(election, candidate, Block(candidate, election));
        await context.SaveChangesAsync();

        Assert.Equal(DeleteCandidateStatus.HasVotes, await Admin(context).DeleteCandidateAsync(candidate.Id));
    }

    [Fact]
    public async Task Election_forms_without_dates_or_with_an_end_before_the_start_are_rejected()
    {
        using var context = _db.CreateContext();
        var ongoing = TestData.OngoingElection();
        context.Add(ongoing);
        await context.SaveChangesAsync();
        var service = Admin(context);

        Assert.Equal(AddElectionStatus.InvalidDates, await service.AddElectionAsync(new ElectionFormViewModel { Opis = "X", DataZakonczenia = TestData.Now }));
        Assert.Equal(UpdateElectionStatus.InvalidDates, await service.UpdateElectionAsync(ongoing.Id, new ElectionFormViewModel { Opis = "X", DataRozpoczecia = TestData.Now }));
        Assert.Equal(AddCandidateStatus.ElectionNotFound, await service.AddCandidateAsync(new CandidateFormViewModel { Imie = "A", Nazwisko = "B", IdWybory = 999 }));
    }

    [Fact]
    public async Task Registration_rejects_invalid_email_and_reports_a_race_as_taken_email()
    {
        var registration = new RegisterViewModel
        {
            Imie = "Anna",
            Nazwisko = "Nowak",
            Email = "to nie jest adres",
            Pesel = "02070803628",
            DataUrodzenia = new DateTime(1995, 5, 5),
            Haslo = "Secret#Pass1",
            ConfirmPassword = "Secret#Pass1",
        };
        using (var context = _db.CreateContext())
        {
            Assert.Equal(RegistrationStatus.InvalidEmail, await Users(context).RegisterAsync(registration, _ => "x"));
        }

        registration.Email = "anna@example.com";
        using (var context = _db.CreateContext(new FailingSaveInterceptor(FailingSaveInterceptor.Pending<Uzytkownik>(EntityState.Added), UniqueViolation)))
        {
            Assert.Equal(RegistrationStatus.EmailTaken, await Users(context).RegisterAsync(registration, _ => "x"));
        }

        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task Empty_reset_token_is_rejected_without_a_database_lookup()
    {
        using var context = _db.CreateContext();
        var service = Users(context);

        Assert.False(await service.IsPasswordResetTokenValidAsync(Guid.Empty));
        Assert.False(await service.ResetPasswordAsync(Guid.Empty, "Brand#New1"));
    }

    [Fact]
    public async Task Vote_that_keeps_losing_the_race_gives_up_after_three_attempts()
    {
        var failing = new FailingSaveInterceptor(FailingSaveInterceptor.Pending<GlosowanieWyborcze>(EntityState.Added), () => new DbUpdateConcurrencyException("version changed"), times: 10);
        using var context = _db.CreateContext(failing);
        var (user, election, candidate) = await SeedVotingAsync(context);

        var outcome = await TestData.Election(context, _clock, _email).CastVoteAsync(user.Id, election.Id, candidate.Id);

        Assert.Equal(VoteStatus.Conflict, outcome.Status);
        Assert.Equal(3, failing.Failures);
        Assert.Empty(context.GlosowanieWyborczes);
    }

    [Fact]
    public async Task Vote_that_hits_a_unique_index_of_another_vote_is_retried()
    {
        var failing = new FailingSaveInterceptor(FailingSaveInterceptor.Pending<GlosowanieWyborcze>(EntityState.Added), UniqueViolation);
        using var context = _db.CreateContext(failing);
        var (user, election, candidate) = await SeedVotingAsync(context);

        var outcome = await TestData.Election(context, _clock, _email).CastVoteAsync(user.Id, election.Id, candidate.Id);

        Assert.Equal(VoteStatus.Success, outcome.Status);
        Assert.Equal(1, failing.Failures);
        Assert.Single(context.GlosowanieWyborczes);
    }

    [Fact]
    public async Task Double_submit_from_two_tabs_ends_as_already_voted()
    {
        // The other tab's vote commits while this one is saving: the unique index on (user, election) refuses the
        // save and, after the rollback, the participation row of the other tab is visible.
        var failing = new FailingSaveInterceptor(FailingSaveInterceptor.Pending<GlosUzytkownika>(EntityState.Added), UniqueViolation);
        var otherTab = new AfterRollback(async connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO GlosUzytkownika (id_uzytkownik, id_wybory, dataOddania) VALUES (1, 1, '2026-06-01')";
            await command.ExecuteNonQueryAsync();
        });
        using var context = _db.CreateContext(failing, otherTab);
        var (user, election, candidate) = await SeedVotingAsync(context);

        var outcome = await TestData.Election(context, _clock, _email).CastVoteAsync(user.Id, election.Id, candidate.Id);

        Assert.Equal(VoteStatus.AlreadyVoted, outcome.Status);
        Assert.Empty(context.GlosowanieWyborczes);
    }

    [Fact]
    public async Task Failed_rollback_after_a_lost_race_does_not_stop_the_retry()
    {
        // A deadlock victim's transaction is already gone on SQL Server, so the rollback itself fails.
        var failing = new FailingSaveInterceptor(FailingSaveInterceptor.Pending<GlosowanieWyborcze>(EntityState.Added), () => new DbUpdateConcurrencyException("deadlock victim"));
        using var context = _db.CreateContext(failing, new FailingRollback());
        var (user, election, candidate) = await SeedVotingAsync(context);

        var outcome = await TestData.Election(context, _clock, _email).CastVoteAsync(user.Id, election.Id, candidate.Id);

        Assert.Equal(VoteStatus.Success, outcome.Status);
        Assert.Single(context.GlosowanieWyborczes);
    }

    [Fact]
    public async Task Vote_is_refused_when_the_stored_head_does_not_match_the_chain()
    {
        using var context = _db.CreateContext();
        var (user, election, candidate) = await SeedVotingAsync(context);
        await context.Database.ExecuteSqlAsync($"UPDATE DataWyborow SET liczbaBlokow = 5 WHERE id = {election.Id}");
        context.ChangeTracker.Clear();

        var outcome = await TestData.Election(context, _clock, _email).CastVoteAsync(user.Id, election.Id, candidate.Id);

        Assert.Equal(VoteStatus.ChainCorrupted, outcome.Status);
        Assert.Empty(context.GlosowanieWyborczes);
        Assert.False(context.Weryfikacje.Single().Poprawny);
    }

    [Fact]
    public async Task Unknown_election_has_no_voting_page_and_no_results()
    {
        using var context = _db.CreateContext();

        Assert.Null(await TestData.Election(context, _clock, _email).GetVotingPageAsync(999));
        Assert.Null(await TestData.Results(context, _clock, _email).GetResultsAsync(999));
    }

    [Fact]
    public async Task Forged_signature_is_named_in_the_verification_details()
    {
        using var context = _db.CreateContext();
        var (user, election, candidate) = await SeedVotingAsync(context);
        await TestData.Election(context, _clock, _email).CastVoteAsync(user.Id, election.Id, candidate.Id);
        var block = context.GlosowanieWyborczes.Single();
        block.Podpis = Convert.ToBase64String(new byte[64]);
        await context.SaveChangesAsync();

        var result = await TestData.Chain(context, _clock, _email).VerifyAndStoreAsync(election.Id, "Manual");

        Assert.False(result.IsValid);
        Assert.Contains(block.Id, result.InvalidSignatureBlockIds);
        Assert.Contains("podpisy", context.Weryfikacje.Single().Szczegoly, StringComparison.Ordinal);
    }

    private async Task<(Uzytkownik User, DataWyborow Election, Kandydat Candidate)> SeedVotingAsync(InternetVotingContext context)
    {
        var user = TestData.User();
        var election = TestData.OngoingElection();
        var candidate = new Kandydat { Imie = "Adam", Nazwisko = "A", IdWyboryNavigation = election };
        context.AddRange(user, election, candidate);
        await context.SaveChangesAsync();
        return (user, election, candidate);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    /// <summary>Runs an action on the connection right after the first rollback, outside the transaction.</summary>
    private sealed class AfterRollback(Func<DbConnection, Task> action) : DbTransactionInterceptor
    {
        private bool _done;

        public override async Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!_done)
            {
                _done = true;
                await action(eventData.Context!.Database.GetDbConnection());
            }
        }
    }

    /// <summary>The first explicit rollback fails, as it does for a transaction the server already rolled back.</summary>
    private sealed class FailingRollback : DbTransactionInterceptor
    {
        private bool _failed;

        public override ValueTask<InterceptionResult> TransactionRollingBackAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!_failed)
            {
                _failed = true;
                throw new InvalidOperationException("This SqlTransaction has completed; it is no longer usable.");
            }

            return base.TransactionRollingBackAsync(transaction, eventData, result, cancellationToken);
        }
    }
}

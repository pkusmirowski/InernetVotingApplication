using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services.Mail;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace InternetVotingApplication.Tests
{
    /// <summary>Records every message instead of sending it.</summary>
    public sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    /// <summary>An in-memory SQLite database that lives as long as the connection is open.</summary>
    public sealed class SqliteDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        public SqliteDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public InternetVotingContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<InternetVotingContext>()
                .UseSqlite(_connection)
                .Options;
            return new InternetVotingContext(options);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }

    public static class TestData
    {
        public static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0);

        public static FakeTimeProvider Clock() => new(new DateTimeOffset(Now, TimeSpan.Zero));

        public static NullLogger<T> Logger<T>() => NullLogger<T>.Instance;

        public static Uzytkownik User(string email = "jan@example.com", string pesel = "44051401359", bool active = true)
        {
            return new Uzytkownik
            {
                Imie = "Jan",
                Nazwisko = "Kowalski",
                Email = email,
                Pesel = pesel,
                DataUrodzenia = new DateTime(1990, 1, 1),
                Haslo = BCrypt.Net.BCrypt.HashPassword("Correct#Horse1"),
                JestAktywne = active,
                KodAktywacyjny = active ? null : Guid.NewGuid(),
                DataRejestracji = Now.AddDays(-10),
            };
        }

        public static DataWyborow OngoingElection(string name = "Wybory testowe")
        {
            return new DataWyborow
            {
                Opis = name,
                DataRozpoczecia = Now.AddDays(-1),
                DataZakonczenia = Now.AddDays(1),
            };
        }
    }
}

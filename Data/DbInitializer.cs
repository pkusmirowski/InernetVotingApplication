using InternetVotingApplication.Configuration;
using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Data;

/// <summary>
/// Start-up work on the database: schema (migrations or EnsureCreated), promotion of configured
/// administrators and, in development, sample elections to click through.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

        using var scope = services.CreateScope();

        // First use of DatabaseInfo: runs the provider decision (and the SQL Server probe) with logging available.
        var databaseInfo = scope.ServiceProvider.GetRequiredService<DatabaseInfo>();
        var context = scope.ServiceProvider.GetRequiredService<InternetVotingContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbInitializer));
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        try
        {
            if (databaseInfo.IsSqlite || databaseOptions.EnsureCreatedOnStartup)
            {
                // SQL Server migrations never run on SQLite: the schema is created straight from the model.
                await context.Database.EnsureCreatedAsync();
            }
            else if (databaseOptions.ApplyMigrationsOnStartup)
            {
                logger.LogInformation("Applying pending database migrations");
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !databaseInfo.IsSqlite)
        {
            var number = (ex as Microsoft.Data.SqlClient.SqlException)?.Number ?? (ex.InnerException as Microsoft.Data.SqlClient.SqlException)?.Number;
            throw new DatabaseUnavailableException(DatabaseProviderResolver.Explain(databaseInfo.ConnectionString, ex.Message, number), ex);
        }

        var seeding = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>().Value;
        await PromoteAdministratorsAsync(context, seeding.AdminEmails, timeProvider, logger);

        if (seeding.SampleData)
        {
            await SeedSampleDataAsync(context, timeProvider, logger);
        }

        if (seeding.TestAccounts)
        {
            await SeedTestAccountsAsync(context, timeProvider, logger);
        }
    }

    /// <summary>
    /// Creates the accounts from <see cref="TestAccounts.All"/> that do not exist yet (matched by e-mail), already activated,
    /// and makes the administrator among them an administrator. Returns how many accounts were created.
    /// </summary>
    public static async Task<int> SeedTestAccountsAsync(InternetVotingContext context, TimeProvider timeProvider, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        var emails = TestAccounts.All.Select(a => a.Email).ToList();
        var existing = await context.Uzytkowniks
            .Where(u => emails.Contains(u.Email))
            .Select(u => new { u.Email, u.Id, IsAdmin = u.Administrators.Any() })
            .ToListAsync();
        var now = timeProvider.GetLocalNow().DateTime;
        var created = 0;

        foreach (var account in TestAccounts.All)
        {
            var found = existing.SingleOrDefault(u => u.Email == account.Email);
            if (found != null)
            {
                if (account.IsAdmin && !found.IsAdmin)
                {
                    context.Administrators.Add(new Administrator { IdUzytkownik = found.Id });
                }

                continue;
            }

            var user = new Uzytkownik
            {
                Imie = account.Imie,
                Nazwisko = account.Nazwisko,
                Pesel = account.Pesel,
                Email = account.Email,
                DataUrodzenia = account.DataUrodzenia,
                Haslo = BCrypt.Net.BCrypt.HashPassword(account.Password),
                JestAktywne = true,
                KodAktywacyjny = null,
                DataRejestracji = now,
            };
            context.Uzytkowniks.Add(user);
            if (account.IsAdmin)
            {
                context.Administrators.Add(new Administrator { IdUzytkownikNavigation = user });
            }

            created++;
        }

        if (context.ChangeTracker.HasChanges())
        {
            context.DziennikAudytu.Add(new DziennikAudytu
            {
                Data = now,
                Akcja = "TestAccountsSeeded",
                Szczegoly = $"Utworzono {created} kont testowych (Seeding:TestAccounts)",
            });
            await context.SaveChangesAsync();
            logger.LogWarning("Created {Count} development test accounts; passwords are listed on /setup and in README", created);
        }

        return created;
    }

    public static async Task PromoteAdministratorsAsync(InternetVotingContext context, IEnumerable<string> adminEmails, TimeProvider timeProvider, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(adminEmails);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        var emails = adminEmails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (emails.Count == 0)
        {
            return;
        }

        var users = await context.Uzytkowniks
            .Where(u => emails.Contains(u.Email))
            .Select(u => new { u.Id, u.Email, IsAdmin = u.Administrators.Any() })
            .ToListAsync();

        foreach (var user in users.Where(u => !u.IsAdmin))
        {
            context.Administrators.Add(new Administrator { IdUzytkownik = user.Id });
            context.DziennikAudytu.Add(new DziennikAudytu
            {
                Data = timeProvider.GetLocalNow().DateTime,
                Akcja = "AdminPromoted",
                Szczegoly = $"{user.Email} (konfiguracja Seeding:AdminEmails)",
                IdUzytkownik = user.Id,
            });
            logger.LogInformation("Promoted {Email} to administrator", user.Email);
        }

        foreach (var missing in emails.Except(users.Select(u => u.Email)))
        {
            logger.LogWarning("Configured administrator {Email} has no account yet; register and activate it first", missing);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Creates one ongoing, one upcoming and one ended election with candidates when there are no elections at all.</summary>
    public static async Task<bool> SeedSampleDataAsync(InternetVotingContext context, TimeProvider timeProvider, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        if (await context.DataWyborows.AnyAsync())
        {
            return false;
        }

        var now = timeProvider.GetLocalNow().DateTime;

        var ongoing = new DataWyborow
        {
            Opis = "Wybory Prezydenckie 2026 (przykładowe, trwające)",
            DataRozpoczecia = now.AddDays(-1),
            DataZakonczenia = now.AddDays(14),
        };
        var upcoming = new DataWyborow
        {
            Opis = "Wybory Samorządowe (przykładowe, nadchodzące)",
            DataRozpoczecia = now.AddDays(30),
            DataZakonczenia = now.AddDays(31),
        };
        var ended = new DataWyborow
        {
            Opis = "Referendum (przykładowe, zakończone)",
            DataRozpoczecia = now.AddDays(-30),
            DataZakonczenia = now.AddDays(-29),
        };

        context.DataWyborows.AddRange(ongoing, upcoming, ended);
        context.Kandydats.AddRange(
            new Kandydat { Imie = "Anna", Nazwisko = "Kowalska", IdWyboryNavigation = ongoing },
            new Kandydat { Imie = "Jan", Nazwisko = "Nowak", IdWyboryNavigation = ongoing },
            new Kandydat { Imie = "Piotr", Nazwisko = "Wiśniewski", IdWyboryNavigation = ongoing },
            new Kandydat { Imie = "Maria", Nazwisko = "Wójcik", IdWyboryNavigation = upcoming },
            new Kandydat { Imie = "Tomasz", Nazwisko = "Kamiński", IdWyboryNavigation = upcoming },
            new Kandydat { Imie = "Za", Nazwisko = "Propozycją", IdWyboryNavigation = ended },
            new Kandydat { Imie = "Przeciw", Nazwisko = "Propozycji", IdWyboryNavigation = ended });

        await context.SaveChangesAsync();
        logger.LogInformation("Sample elections created");
        return true;
    }
}

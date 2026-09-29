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
        else if (!scope.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            // A database first used in development may still hold the well-known test accounts. An unreachable
            // database must not stop the start-up here; every request would report it anyway.
            try
            {
                await DisableTestAccountsAsync(context, timeProvider, logger);
            }
            catch (Exception ex) when (ex is System.Data.Common.DbException or InvalidOperationException)
            {
                logger.LogWarning(ex, "Could not check the database for development test accounts");
            }
        }
    }

    /// <summary>
    /// Deactivates the accounts from <see cref="TestAccounts.All"/> and removes their administrator role: their
    /// passwords are public, so they must not work outside development. Returns how many accounts were changed.
    /// </summary>
    public static async Task<int> DisableTestAccountsAsync(InternetVotingContext context, TimeProvider timeProvider, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        var emails = TestAccounts.All.Select(a => a.Email).ToList();
        var users = await context.Uzytkowniks
            .Include(u => u.Administrators)
            .Where(u => emails.Contains(u.Email) && (u.JestAktywne || u.Administrators.Any()))
            .ToListAsync();
        if (users.Count == 0)
        {
            return 0;
        }

        foreach (var user in users)
        {
            user.JestAktywne = false;
            context.Administrators.RemoveRange(user.Administrators);
        }

        context.DziennikAudytu.Add(new DziennikAudytu
        {
            Data = timeProvider.GetLocalNow().DateTime,
            Akcja = "TestAccountsDisabled",
            Szczegoly = $"Wyłączono {users.Count} kont testowych poza środowiskiem Development",
        });
        await context.SaveChangesAsync();
        logger.LogWarning("Disabled {Count} development test accounts found outside the Development environment", users.Count);
        return users.Count;
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

        var firstRound = new DataWyborow
        {
            Opis = "Wybory Prezydenckie 2025, I tura (przykładowe, zakończone)",
            DataRozpoczecia = now.AddDays(-15),
            DataZakonczenia = now.AddDays(-14),
        };
        var secondRound = new DataWyborow
        {
            Opis = "Wybory Prezydenckie 2025, II tura (przykładowe, trwające)",
            DataRozpoczecia = now.AddDays(-1),
            DataZakonczenia = now.AddDays(14),
        };
        var nextElection = new DataWyborow
        {
            Opis = "Wybory Prezydenckie 2030 (przykładowe, nadchodzące)",
            DataRozpoczecia = now.AddDays(30),
            DataZakonczenia = now.AddDays(31),
        };

        context.DataWyborows.AddRange(firstRound, secondRound, nextElection);
        context.Kandydats.AddRange(
            new Kandydat { Imie = "Anna", Nazwisko = "Kowalska", IdWyboryNavigation = firstRound },
            new Kandydat { Imie = "Jan", Nazwisko = "Nowak", IdWyboryNavigation = firstRound },
            new Kandydat { Imie = "Piotr", Nazwisko = "Wiśniewski", IdWyboryNavigation = firstRound },
            new Kandydat { Imie = "Anna", Nazwisko = "Kowalska", IdWyboryNavigation = secondRound },
            new Kandydat { Imie = "Jan", Nazwisko = "Nowak", IdWyboryNavigation = secondRound },
            new Kandydat { Imie = "Maria", Nazwisko = "Wójcik", IdWyboryNavigation = nextElection },
            new Kandydat { Imie = "Tomasz", Nazwisko = "Kamiński", IdWyboryNavigation = nextElection });

        await context.SaveChangesAsync();
        logger.LogInformation("Sample elections created");
        return true;
    }
}

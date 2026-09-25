using InternetVotingApplication.Configuration;
using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Data
{
    /// <summary>
    /// Applies migrations (when enabled) and promotes configured accounts to administrators.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
        {
            var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<InternetVotingContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbInitializer));

            if (databaseOptions.EnsureCreatedOnStartup)
            {
                await context.Database.EnsureCreatedAsync();
            }
            else if (databaseOptions.ApplyMigrationsOnStartup)
            {
                logger.LogInformation("Applying pending database migrations");
                await context.Database.MigrateAsync();
            }

            var seeding = scope.ServiceProvider.GetRequiredService<IOptions<SeedingOptions>>().Value;
            await PromoteAdministratorsAsync(context, seeding.AdminEmails, logger);
        }

        public static async Task PromoteAdministratorsAsync(InternetVotingContext context, IEnumerable<string> adminEmails, ILogger logger)
        {
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
                logger.LogInformation("Promoted {Email} to administrator", user.Email);
            }

            foreach (var missing in emails.Except(users.Select(u => u.Email)))
            {
                logger.LogWarning("Configured administrator {Email} has no account yet; register and activate it first", missing);
            }

            await context.SaveChangesAsync();
        }
    }
}

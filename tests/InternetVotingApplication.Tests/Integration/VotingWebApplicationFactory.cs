using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternetVotingApplication.Tests.Integration
{
    /// <summary>
    /// Boots the real application on an in-memory SQLite database with e-mail delivery captured.
    /// </summary>
    public sealed class VotingWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public FakeEmailSender Emails { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ApplyMigrationsOnStartup"] = "false",
                    ["Database:EnsureCreatedOnStartup"] = "true",
                    ["Smtp:Enabled"] = "false",
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InternetVotingContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<InternetVotingContext>>();
                services.RemoveAll<InternetVotingContext>();
                services.AddDbContext<InternetVotingContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
            });
        }

        public HttpClient CreateHttpsClient(bool allowAutoRedirect = false)
        {
            return CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = allowAutoRedirect,
            });
        }

        public InternetVotingContext CreateContext()
        {
            var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<InternetVotingContext>();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}

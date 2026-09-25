using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Tests.Services
{
    public sealed class ChainVerificationWorkerTests : IDisposable
    {
        private readonly SqliteDatabase _db = new();
        private readonly FakeEmailSender _email = new();
        private readonly Microsoft.Extensions.Time.Testing.FakeTimeProvider _clock = TestData.Clock();

        private ChainVerificationWorker CreateWorker()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<TimeProvider>(_clock);
            services.AddSingleton(TestData.Signer);
            services.AddSingleton<IEmailSender>(_email);
            services.AddSingleton(TestData.ChainOptions(recipients: ["komisja@example.com"]));
            services.AddScoped(_ => _db.CreateContext());
            services.AddScoped<IAuditLog, AuditLog>();
            services.AddScoped<IChainService, ChainService>();
            var provider = services.BuildServiceProvider();
            return new ChainVerificationWorker(provider.GetRequiredService<IServiceScopeFactory>(), TestData.ChainOptions(), _clock, TestData.Logger<ChainVerificationWorker>());
        }

        [Fact]
        public async Task Cycle_verifies_active_chains_and_publishes_final_anchor_once()
        {
            using var context = _db.CreateContext();
            var ongoing = TestData.OngoingElection("Trwające");
            var ended = new DataWyborow { Opis = "Zakończone", DataRozpoczecia = TestData.Now.AddDays(-3), DataZakonczenia = TestData.Now.AddDays(-1) };
            context.AddRange(ongoing, ended);
            await context.SaveChangesAsync();
            var worker = CreateWorker();

            await worker.RunOnceAsync(CancellationToken.None);

            using var check = _db.CreateContext();
            Assert.Equal(2, await check.Weryfikacje.CountAsync());
            Assert.All(await check.Weryfikacje.ToListAsync(), v => Assert.Equal("Background", v.Wyzwalacz));
            var anchor = Assert.Single(await check.Kotwice.ToListAsync());
            Assert.Equal(ended.Id, anchor.IdWybory);
            Assert.Equal(ChainService.ReasonElectionEnded, anchor.Powod);
            Assert.Single(_email.Sent);

            await worker.RunOnceAsync(CancellationToken.None);
            using var again = _db.CreateContext();
            Assert.Equal(4, await again.Weryfikacje.CountAsync());
            Assert.Equal(1, await again.Kotwice.CountAsync());
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}

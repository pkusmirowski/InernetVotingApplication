using InternetVotingApplication.Configuration;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Tests.Services
{
    public sealed class OutboxTests : IDisposable
    {
        private readonly SqliteDatabase _db = new();
        private readonly Microsoft.Extensions.Time.Testing.FakeTimeProvider _clock = TestData.Clock();

        private EmailDispatcher CreateDispatcher(FakeSmtpTransport transport, int maxAttempts = 3)
        {
            var services = new ServiceCollection();
            services.AddSingleton<TimeProvider>(_clock);
            services.AddScoped(_ => _db.CreateContext());
            services.AddScoped<EmailQueue>();
            var provider = services.BuildServiceProvider();
            var options = Options.Create(new MailOptions { MaxAttempts = maxAttempts, BatchSize = 10 });
            return new EmailDispatcher(provider.GetRequiredService<IServiceScopeFactory>(), transport, options, TestData.Logger<EmailDispatcher>());
        }

        [Fact]
        public async Task Queued_sender_persists_message_and_dispatcher_delivers_it()
        {
            using var context = _db.CreateContext();
            var sender = new QueuedEmailSender(new EmailQueue(context, _clock));
            await sender.SendAsync(new EmailMessage("a@example.com", "Temat", "<p>Treść</p>"));

            var row = Assert.Single(context.WiadomosciEmail);
            Assert.Null(row.Wyslano);

            var transport = new FakeSmtpTransport();
            var sent = await CreateDispatcher(transport).ProcessOnceAsync(CancellationToken.None);

            Assert.Equal(1, sent);
            var delivered = Assert.Single(transport.Sent);
            Assert.Equal("a@example.com", delivered.To);
            using var check = _db.CreateContext();
            var stored = await check.WiadomosciEmail.SingleAsync();
            Assert.NotNull(stored.Wyslano);
            Assert.Equal(1, stored.Proby);
        }

        [Fact]
        public async Task Failed_delivery_is_retried_with_backoff_and_gives_up_after_max_attempts()
        {
            using var context = _db.CreateContext();
            await new QueuedEmailSender(new EmailQueue(context, _clock)).SendAsync(new EmailMessage("a@example.com", "T", "B"));
            var transport = new FakeSmtpTransport { Fail = true };
            var dispatcher = CreateDispatcher(transport, maxAttempts: 2);

            Assert.Equal(0, await dispatcher.ProcessOnceAsync(CancellationToken.None));
            using (var check = _db.CreateContext())
            {
                var row = await check.WiadomosciEmail.SingleAsync();
                Assert.Equal(1, row.Proby);
                Assert.Equal("SMTP down", row.OstatniBlad);
                Assert.True(row.NastepnaProba > _clock.GetLocalNow().DateTime);
            }

            // Not due yet.
            Assert.Equal(0, await dispatcher.ProcessOnceAsync(CancellationToken.None));
            _clock.Advance(TimeSpan.FromMinutes(5));
            Assert.Equal(0, await dispatcher.ProcessOnceAsync(CancellationToken.None));

            // Max attempts reached: no more tries even when SMTP recovers.
            transport.Fail = false;
            _clock.Advance(TimeSpan.FromHours(1));
            Assert.Equal(0, await dispatcher.ProcessOnceAsync(CancellationToken.None));
            Assert.Empty(transport.Sent);
        }

        [Fact]
        public async Task Message_written_inside_a_rolled_back_transaction_is_never_sent()
        {
            using var context = _db.CreateContext();
            await using (var tx = await context.Database.BeginTransactionAsync())
            {
                await new QueuedEmailSender(new EmailQueue(context, _clock)).SendAsync(new EmailMessage("a@example.com", "T", "B"));
                await tx.RollbackAsync();
            }

            using var check = _db.CreateContext();
            Assert.Empty(check.WiadomosciEmail);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}

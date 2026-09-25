using InternetVotingApplication.Configuration;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Services.Mail
{
    /// <summary>
    /// Background worker that drains the outbox with retries and exponential back-off. A failed delivery is
    /// logged and retried; it never affects the HTTP request that produced the message.
    /// </summary>
    public sealed class EmailDispatcher(
        IServiceScopeFactory scopeFactory,
        ISmtpTransport transport,
        IOptions<MailOptions> options,
        ILogger<EmailDispatcher> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(options.Value.PollInterval);
            do
            {
                try
                {
                    await ProcessOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Outbox processing failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        /// <summary>Sends one batch of due messages. Public for tests.</summary>
        public async Task<int> ProcessOnceAsync(CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<EmailQueue>();
            var batch = await queue.TakeDueAsync(options.Value.BatchSize, options.Value.MaxAttempts, cancellationToken);
            var sent = 0;

            foreach (var row in batch)
            {
                try
                {
                    await transport.SendAsync(new EmailMessage(row.Odbiorca, row.Temat, row.Tresc), cancellationToken);
                    queue.MarkSent(row);
                    sent++;
                    logger.LogInformation("E-mail {Id} '{Subject}' sent to {To}", row.Id, row.Temat, row.Odbiorca);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    queue.MarkFailed(row, ex.Message);
                    logger.LogWarning(ex, "E-mail {Id} to {To} failed (attempt {Attempt})", row.Id, row.Odbiorca, row.Proby);
                }
            }

            if (batch.Count > 0)
            {
                await queue.SaveAsync(cancellationToken);
            }

            return sent;
        }
    }
}

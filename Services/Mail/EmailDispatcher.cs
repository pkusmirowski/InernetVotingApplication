namespace InternetVotingApplication.Services.Mail
{
    /// <summary>
    /// Background worker that drains <see cref="EmailQueue"/>. A failed delivery is logged and never
    /// affects the HTTP request that produced the message.
    /// </summary>
    public sealed class EmailDispatcher(EmailQueue queue, SmtpEmailSender sender, ILogger<EmailDispatcher> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await sender.SendAsync(message, stoppingToken);
                    logger.LogInformation("E-mail '{Subject}' sent to {To}", message.Subject, message.To);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send e-mail '{Subject}' to {To}", message.Subject, message.To);
                }
            }
        }
    }
}

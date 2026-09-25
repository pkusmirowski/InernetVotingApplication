using InternetVotingApplication.Interfaces;

namespace InternetVotingApplication.Services.Mail
{
    /// <summary>
    /// <see cref="IEmailSender"/> used by the application: puts the message on the queue and returns immediately.
    /// </summary>
    public sealed class QueuedEmailSender(EmailQueue queue) : IEmailSender
    {
        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            await queue.EnqueueAsync(message, cancellationToken);
        }
    }
}

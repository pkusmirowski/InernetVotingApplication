using InternetVotingApplication.Interfaces;

namespace InternetVotingApplication.Services.Mail
{
    /// <summary>
    /// <see cref="IEmailSender"/> used by the application: stores the message in the outbox table and saves.
    /// Inside an open transaction the row commits together with the business change.
    /// </summary>
    public sealed class QueuedEmailSender(EmailQueue queue) : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            queue.Enqueue(message);
            return queue.SaveAsync(cancellationToken);
        }
    }
}

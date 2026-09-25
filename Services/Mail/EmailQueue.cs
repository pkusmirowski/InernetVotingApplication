using System.Threading.Channels;

namespace InternetVotingApplication.Services.Mail
{
    /// <summary>
    /// In-process queue that decouples HTTP requests from SMTP delivery.
    /// </summary>
    public sealed class EmailQueue
    {
        private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>(
            new UnboundedChannelOptions { SingleReader = true });

        public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(message, cancellationToken);
        }

        public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}

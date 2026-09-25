namespace InternetVotingApplication.Services.Mail
{
    /// <summary>Actual delivery of a single message (SMTP in production, a fake in tests).</summary>
    public interface ISmtpTransport
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}

using InternetVotingApplication.Services.Mail;

namespace InternetVotingApplication.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}

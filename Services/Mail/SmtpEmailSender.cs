using InternetVotingApplication.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace InternetVotingApplication.Services.Mail
{
    /// <summary>
    /// Delivers messages over SMTP with MailKit. Used only by <see cref="EmailDispatcher"/> through <see cref="ISmtpTransport"/>.
    /// </summary>
    public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : ISmtpTransport
    {
        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            var smtp = options.Value;

            if (!smtp.Enabled)
            {
                logger.LogInformation("SMTP disabled; e-mail to {To} with subject '{Subject}' not sent", message.To, message.Subject);
                return;
            }

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;
            mime.Body = new TextPart(TextFormat.Html) { Text = message.HtmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port, ParseSocketOptions(smtp.SecureSocket), cancellationToken);
            if (!string.IsNullOrEmpty(smtp.UserName))
            {
                await client.AuthenticateAsync(smtp.UserName, smtp.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        private static SecureSocketOptions ParseSocketOptions(string? value)
        {
            return Enum.TryParse<SecureSocketOptions>(value, ignoreCase: true, out var parsed)
                ? parsed
                : SecureSocketOptions.Auto;
        }
    }
}

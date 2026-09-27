using InternetVotingApplication.Configuration;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Services.Mail;

/// <summary>Chooses the transport from configuration: pickup directory (development) or real SMTP.</summary>
public static class MailTransportFactory
{
    public static ISmtpTransport Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = services.GetRequiredService<IOptions<SmtpOptions>>();
        return string.IsNullOrWhiteSpace(options.Value.PickupDirectory)
            ? ActivatorUtilities.CreateInstance<SmtpEmailSender>(services)
            : ActivatorUtilities.CreateInstance<PickupDirectoryTransport>(services);
    }
}

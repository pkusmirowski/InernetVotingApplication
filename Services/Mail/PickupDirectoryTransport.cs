using System.Globalization;
using System.Net;
using InternetVotingApplication.Configuration;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Services.Mail;

/// <summary>
/// Development transport: writes every message as an <c>.html</c> file into a pickup directory, so that
/// activation and reset links can be opened without any SMTP server.
/// </summary>
public sealed class PickupDirectoryTransport(IOptions<SmtpOptions> options, IHostEnvironment environment, ILogger<PickupDirectoryTransport> logger) : ISmtpTransport
{
    public string Directory { get; } = ResolveDirectory(options.Value.PickupDirectory, environment.ContentRootPath);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        System.IO.Directory.CreateDirectory(Directory);

        var safeRecipient = string.Concat(message.To.Select(c => char.IsLetterOrDigit(c) || c is '.' or '@' or '-' or '_' ? c : '_'));
        var fileName = $"{DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)}-{safeRecipient}.html";
        var path = Path.Combine(Directory, fileName);

        var html =
            "<!DOCTYPE html><html lang=\"pl\"><head><meta charset=\"utf-8\"><title>" + WebUtility.HtmlEncode(message.Subject) + "</title></head><body>" +
            $"<p><b>Do:</b> {WebUtility.HtmlEncode(message.To)}<br/><b>Temat:</b> {WebUtility.HtmlEncode(message.Subject)}</p><hr/>" +
            message.HtmlBody +
            "</body></html>";

        await File.WriteAllTextAsync(path, html, cancellationToken);
        logger.LogInformation("E-mail to {To} written to {Path}", message.To, path);
    }

    public static string ResolveDirectory(string? configured, string contentRoot)
    {
        var directory = string.IsNullOrWhiteSpace(configured) ? "App_Data/mail" : configured;
        return Path.IsPathRooted(directory) ? directory : Path.Combine(contentRoot, directory);
    }
}

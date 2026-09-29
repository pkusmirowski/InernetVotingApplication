using System.ComponentModel.DataAnnotations;

namespace InternetVotingApplication.Configuration;

/// <summary>
/// Mail delivery settings bound from the <c>Smtp</c> configuration section.
/// Credentials must never be committed; use user secrets or environment variables.
/// </summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>When false, messages are only logged.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// When set, messages are written as <c>.html</c> files to this directory instead of being sent
    /// (local development without an SMTP server). Relative paths resolve against the content root.
    /// </summary>
    public string? PickupDirectory { get; set; }

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 25;

    /// <summary>One of <c>None</c>, <c>Auto</c>, <c>SslOnConnect</c>, <c>StartTls</c>, <c>StartTlsWhenAvailable</c>.</summary>
    public string SecureSocket { get; set; } = "Auto";

    public string? UserName { get; set; }

    public string? Password { get; set; }

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = "no-reply@localhost";

    public string FromName { get; set; } = "Głosowanie internetowe";
}

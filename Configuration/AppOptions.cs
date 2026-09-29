namespace InternetVotingApplication.Configuration;

/// <summary>General application settings bound from the <c>App</c> section.</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Public address of the application (e.g. <c>https://glosowanie.example.pl</c>) used to build the links sent
    /// by e-mail. When empty, the scheme and host of the incoming request are used, which is only safe when the
    /// <c>Host</c> header is trustworthy (development, or a reverse proxy that overrides it).
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Contact e-mail shown in the footer and on the contact page; when empty, no contact details are shown.</summary>
    public string? ContactEmail { get; set; }
}

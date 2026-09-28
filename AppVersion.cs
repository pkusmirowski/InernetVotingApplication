using System.Reflection;

namespace InternetVotingApplication;

/// <summary>Application version taken from the assembly metadata (<c>Version</c> in Directory.Build.props).</summary>
public static class AppVersion
{
    public static string Value { get; } = Read();

    private static string Read()
    {
        var informational = typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational))
        {
            return typeof(AppVersion).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }

        // Strip the "+<commit>" suffix that SourceLink appends.
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? informational : informational[..plus];
    }
}

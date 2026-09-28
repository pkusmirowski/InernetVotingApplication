using InternetVotingApplication.Configuration;
using InternetVotingApplication.Data;

namespace InternetVotingApplication.ViewModels;

public enum SetupState
{
    Ok,
    Warning,
    Error,
}

public sealed record SetupItem(string Label, string Value, SetupState State, string? Hint = null);

/// <summary>Everything the development diagnostics page shows, grouped in sections.</summary>
public sealed class SetupViewModel
{
    public string EnvironmentName { get; set; } = string.Empty;

    public string RuntimeVersion { get; set; } = string.Empty;

    public string ApplicationVersion { get; set; } = string.Empty;

    public string ContentRoot { get; set; } = string.Empty;

    public DatabaseProvider Provider { get; set; }

    public DatabaseSelectionReason Reason { get; set; }

    public string? FallbackExplanation { get; set; }

    public List<SetupItem> Database { get; } = [];

    public List<SetupItem> Mail { get; } = [];

    public List<SetupItem> Signing { get; } = [];

    public List<SetupItem> Data { get; } = [];

    /// <summary>Ordered "what to do next" steps for the person looking at the page.</summary>
    public List<string> NextSteps { get; } = [];

    public List<string> Errors { get; } = [];

    public SetupState OverallState =>
        Database.Concat(Mail).Concat(Signing).Concat(Data).Select(i => i.State).DefaultIfEmpty(SetupState.Ok).Max();
}

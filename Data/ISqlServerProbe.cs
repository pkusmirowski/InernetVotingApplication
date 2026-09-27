namespace InternetVotingApplication.Data;

/// <summary>Outcome of the start-up connectivity probe.</summary>
/// <param name="ErrorNumber">SQL Server error number when available (e.g. 4060, 18456), otherwise null.</param>
public sealed record ProbeResult(bool Ok, string? Error, int? ErrorNumber, TimeSpan Elapsed);

/// <summary>Quick connectivity check used once at start-up; abstracted so the provider selection is testable.</summary>
public interface ISqlServerProbe
{
    ProbeResult CanConnect(string connectionString, TimeSpan timeout);
}

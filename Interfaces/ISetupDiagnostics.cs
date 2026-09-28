using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces;

/// <summary>Collects the state of the running instance for the development diagnostics page.</summary>
public interface ISetupDiagnostics
{
    Task<SetupViewModel> CollectAsync(CancellationToken cancellationToken = default);
}

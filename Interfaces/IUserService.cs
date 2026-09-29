using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces;

public sealed record LoginOutcome(LoginStatus Status, Uzytkownik? User, bool IsAdmin);

/// <summary>What a signed-in session is checked against on every request.</summary>
public sealed record SessionState(bool IsActive, bool IsAdmin, string PasswordStamp);

public interface IUserService
{
    /// <summary>Creates an inactive account and sends the activation e-mail.</summary>
    /// <param name="activationLinkFactory">Builds the absolute activation URL for a given activation code.</param>
    Task<RegistrationStatus> RegisterAsync(RegisterViewModel model, Func<Guid, string> activationLinkFactory);

    Task<LoginOutcome> LoginAsync(Logowanie model);

    Task<bool> ActivateAsync(Guid activationCode);

    Task<bool> ChangePasswordAsync(int userId, ChangePassword model);

    /// <summary>Always completes without revealing whether the account exists.</summary>
    Task RequestPasswordResetAsync(string email, Func<Guid, string> resetLinkFactory);

    Task<bool> IsPasswordResetTokenValidAsync(Guid token);

    Task<bool> ResetPasswordAsync(Guid token, string newPassword);

    /// <summary>Current account state for session validation; null when the account no longer exists.</summary>
    Task<SessionState?> GetSessionStateAsync(int userId);
}

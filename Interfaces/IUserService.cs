using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;

namespace InternetVotingApplication.Interfaces
{
    public sealed record LoginOutcome(LoginStatus Status, Uzytkownik? User, bool IsAdmin);

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
    }
}

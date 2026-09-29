using System.Security.Claims;
using InternetVotingApplication.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace InternetVotingApplication.Services;

/// <summary>
/// Checks every signed-in request against the database, so that account changes reach existing sessions at once:
/// a deactivated account or a changed password signs the session out, a granted or revoked administrator role
/// replaces the role in the cookie.
/// </summary>
public static class SessionValidator
{
    /// <summary>Claim with <see cref="UserService.PasswordStamp"/> of the password the session was opened with.</summary>
    public const string PasswordStampClaim = "pwd";

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var principal = context.Principal;
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var state = int.TryParse(userId, out var id)
            ? await context.HttpContext.RequestServices.GetRequiredService<IUserService>().GetSessionStateAsync(id)
            : null;

        if (principal == null || state == null || !state.IsActive || principal.FindFirstValue(PasswordStampClaim) != state.PasswordStamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var role = state.IsAdmin ? Roles.Admin : Roles.Voter;
        if (!principal.IsInRole(role))
        {
            var claims = principal.Claims.Where(c => c.Type != ClaimTypes.Role).Append(new Claim(ClaimTypes.Role, role));
            context.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            context.ShouldRenew = true;
        }
    }
}

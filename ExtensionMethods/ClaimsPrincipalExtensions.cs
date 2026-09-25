using System.Security.Claims;

namespace InternetVotingApplication.ExtensionMethods
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Returns the database id of the signed-in user stored in the NameIdentifier claim.</summary>
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(principal);
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id)
                ? id
                : throw new InvalidOperationException("The current principal has no user id claim.");
        }
    }
}

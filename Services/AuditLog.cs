using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;

namespace InternetVotingApplication.Services
{
    public sealed class AuditLog(InternetVotingContext context, TimeProvider timeProvider, ILogger<AuditLog> logger) : IAuditLog
    {
        public static class Actions
        {
            public const string ElectionCreated = "ElectionCreated";
            public const string CandidateAdded = "CandidateAdded";
            public const string CandidateDeleted = "CandidateDeleted";
            public const string ChainVerified = "ChainVerified";
            public const string ChainCorrupted = "ChainCorrupted";
            public const string AnchorPublished = "AnchorPublished";
            public const string AdminPromoted = "AdminPromoted";
        }

        public async Task LogAsync(string action, string? details = null, int? userId = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(action);
            context.DziennikAudytu.Add(new DziennikAudytu
            {
                Data = timeProvider.GetLocalNow().DateTime,
                IdUzytkownik = userId,
                Akcja = action,
                Szczegoly = details is { Length: > 2000 } ? details[..2000] : details,
            });
            await context.SaveChangesAsync();
            logger.LogInformation("Audit {Action} by {UserId}: {Details}", action, userId, details);
        }
    }
}

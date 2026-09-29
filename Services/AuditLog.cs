using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;

namespace InternetVotingApplication.Services;

public sealed class AuditLog(InternetVotingContext context, TimeProvider timeProvider, ILogger<AuditLog> logger) : IAuditLog
{
    public static class Actions
    {
        public const string ElectionCreated = "ElectionCreated";
        public const string ElectionUpdated = "ElectionUpdated";
        public const string ElectionDeleted = "ElectionDeleted";
        public const string CandidateAdded = "CandidateAdded";
        public const string CandidateDeleted = "CandidateDeleted";
        public const string ChainVerified = "ChainVerified";
        public const string ChainCorrupted = "ChainCorrupted";
        public const string AnchorPublished = "AnchorPublished";
        public const string AdminPromoted = "AdminPromoted";
        public const string AdminRevoked = "AdminRevoked";
        public const string UserActivated = "UserActivated";

        /// <summary>Plain-language name of an action, for the audit page. Unknown codes are shown as they are.</summary>
        public static string Describe(string? action) => action switch
        {
            ElectionCreated => "Utworzono wybory",
            ElectionUpdated => "Zmieniono wybory",
            ElectionDeleted => "Usunięto wybory",
            CandidateAdded => "Dodano kandydata",
            CandidateDeleted => "Usunięto kandydata",
            ChainVerified => "Kontrola rejestru głosów: bez zastrzeżeń",
            ChainCorrupted => "Kontrola rejestru głosów: wykryto nieprawidłowości",
            AnchorPublished => "Wysłano kopię kontrolną",
            AdminPromoted => "Nadano uprawnienia administratora",
            AdminRevoked => "Odebrano uprawnienia administratora",
            UserActivated => "Aktywowano konto",
            "TestAccountsSeeded" => "Utworzono konta testowe",
            "TestAccountsDisabled" => "Wyłączono konta testowe poza środowiskiem Development",
            _ => action ?? string.Empty,
        };
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

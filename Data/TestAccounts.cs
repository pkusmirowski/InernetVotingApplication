namespace InternetVotingApplication.Data;

/// <summary>One development account created by <see cref="DbInitializer"/> when <c>Seeding:TestAccounts</c> is on.</summary>
public sealed record TestAccount(string Email, string Password, string Imie, string Nazwisko, string Pesel, DateTime DataUrodzenia, bool IsAdmin);

/// <summary>
/// Fixed, well-known development accounts: one administrator and a handful of voters, all activated,
/// so that clicking through the application needs no registration or activation e-mails.
/// Never created outside the Development environment (see <c>Startup</c>).
/// </summary>
public static class TestAccounts
{
    public const string AdminPassword = "Admin123!";

    public const string VoterPassword = "Wyborca123!";

    public static IReadOnlyList<TestAccount> All { get; } =
    [
        new("admin@test.local", AdminPassword, "Adam", "Administrator", "85010112345", new DateTime(1985, 1, 1), IsAdmin: true),
        new("wyborca1@test.local", VoterPassword, "Anna", "Testowa", "90020212347", new DateTime(1990, 2, 2), IsAdmin: false),
        new("wyborca2@test.local", VoterPassword, "Bartosz", "Testowy", "88030312342", new DateTime(1988, 3, 3), IsAdmin: false),
        new("wyborca3@test.local", VoterPassword, "Celina", "Testowa", "92040412347", new DateTime(1992, 4, 4), IsAdmin: false),
        new("wyborca4@test.local", VoterPassword, "Dawid", "Testowy", "85050512347", new DateTime(1985, 5, 5), IsAdmin: false),
        new("wyborca5@test.local", VoterPassword, "Ewa", "Testowa", "91060612346", new DateTime(1991, 6, 6), IsAdmin: false),
    ];
}

using System.Net;
using InternetVotingApplication.Services.Mail;

namespace InternetVotingApplication.ExtensionMethods;

/// <summary>
/// Builds the e-mail messages sent by the application. Sending is done by <see cref="Interfaces.IEmailSender"/>.
/// All user-supplied values are HTML-encoded.
/// </summary>
public static class Email
{
    public static EmailMessage AfterRegistration(string to, string firstName, string lastName, string activationLink)
    {
        var body =
            $"<h2>Dzień dobry, {Enc(firstName)} {Enc(lastName)}</h2>" +
            "<p>Twoje konto w aplikacji do głosowania jest już założone. Został ostatni krok: kliknij poniższy link, żeby je aktywować. Dopiero potem można się zalogować i głosować.</p>" +
            $"<p><a href=\"{Enc(activationLink)}\">Aktywuj konto</a></p>" +
            "<p>Jeśli to nie Ty zakładasz konto, zignoruj tę wiadomość. Bez kliknięcia linku konto pozostanie nieaktywne.</p>";
        return new EmailMessage(to, "Aktywacja konta w aplikacji do głosowania", body);
    }

    /// <summary>Confirmation of a cast vote with the receipt code the voter can look up later.</summary>
    /// <param name="searchLink">Absolute link to the vote search with the code filled in; omitted when no public address is configured.</param>
    public static EmailMessage VoteReceipt(string to, string electionName, string hash, string? searchLink = null)
    {
        var check = string.IsNullOrEmpty(searchLink)
            ? "<p>Zachowaj tę wiadomość. Żeby sprawdzić, czy Twój głos jest zapisany i czy nikt go nie zmienił, otwórz w aplikacji zakładkę „Sprawdź głos” i wklej powyższy kod.</p>"
            : $"<p>Zachowaj tę wiadomość. Żeby sprawdzić, czy Twój głos jest zapisany i czy nikt go nie zmienił, otwórz <a href=\"{Enc(searchLink)}\">sprawdzenie głosu</a> albo wklej powyższy kod w zakładce „Sprawdź głos”.</p>";
        var body =
            "<h2>Dziękujemy, Twój głos został zapisany</h2>" +
            $"<p>Wybory: <b>{Enc(electionName)}</b></p>" +
            $"<p>Twój kod potwierdzenia:<br><b>{Enc(hash)}</b></p>" +
            check +
            "<p>Nie przekazuj kodu innym osobom. Każdy, kto go zna, może sprawdzić, na kogo oddano ten głos.</p>";
        return new EmailMessage(to, "Potwierdzenie oddania głosu", body);
    }

    public static EmailMessage PasswordChanged(string to)
    {
        const string body =
            "<h2>Hasło do Twojego konta zostało zmienione</h2>" +
            "<p>Jeśli to Ty, nie musisz nic robić.</p>" +
            "<p>Jeśli to nie Ty, ktoś mógł uzyskać dostęp do Twojego konta. Jak najszybciej ustaw nowe hasło opcją „Przypomnij hasło” na stronie logowania i skontaktuj się z nami.</p>";
        return new EmailMessage(to, "Zmiana hasła", body);
    }

    public static EmailMessage PasswordReset(string to, string resetLink, TimeSpan validFor)
    {
        var minutes = (int)Math.Round(validFor.TotalMinutes);
        var body =
            "<h2>Ustawianie nowego hasła</h2>" +
            $"<p>Kliknij poniższy link, żeby ustawić nowe hasło. Link jest ważny przez {minutes} minut i działa tylko raz.</p>" +
            $"<p><a href=\"{Enc(resetLink)}\">Ustaw nowe hasło</a></p>" +
            "<p>Jeśli ta prośba nie pochodzi od Ciebie, zignoruj tę wiadomość. Twoje hasło pozostaje bez zmian.</p>";
        return new EmailMessage(to, "Nowe hasło w aplikacji do głosowania", body);
    }

    public static EmailMessage ChainAnchor(string to, string electionName, int electionId, Models.KotwicaLancucha anchor, string keyId)
    {
        ArgumentNullException.ThrowIfNull(anchor);
        var body =
            $"<h2>Kopia kontrolna rejestru głosów: {Enc(electionName)} (id {electionId})</h2>" +
            "<p>To automatyczna wiadomość dla komisji wyborczej. Zawiera podpisany stan rejestru głosów z chwili wysłania.</p>" +
            "<p><b>Zachowaj tę wiadomość.</b> Jeśli ktoś później zmieni lub usunie zapisane głosy, porównanie rejestru z tą kopią to wykaże.</p>" +
            "<table>" +
            $"<tr><td>Data i godzina</td><td>{anchor.Data:yyyy-MM-dd HH:mm:ss.fffffff}</td></tr>" +
            $"<tr><td>Liczba głosów w rejestrze</td><td>{anchor.LiczbaBlokow}</td></tr>" +
            $"<tr><td>Kod ostatniego wpisu (hash głowy łańcucha)</td><td><code>{Enc(anchor.HashGlowy ?? "(pusty rejestr)")}</code></td></tr>" +
            $"<tr><td>Rodzaj kopii</td><td>{Enc(Services.ChainService.DescribeReason(anchor.Powod))}</td></tr>" +
            $"<tr><td>Identyfikator klucza podpisu</td><td>{Enc(keyId)}</td></tr>" +
            $"<tr><td>Podpis elektroniczny (ECDSA P-256, base64)</td><td><code>{Enc(anchor.Podpis)}</code></td></tr>" +
            "</table>";
        return new EmailMessage(to, $"Kopia kontrolna rejestru głosów: {electionName}", body);
    }

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}

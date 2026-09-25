using InternetVotingApplication.Services.Mail;
using System.Net;

namespace InternetVotingApplication.ExtensionMethods
{
    /// <summary>
    /// Builds the e-mail messages sent by the application. Sending is done by <see cref="Interfaces.IEmailSender"/>.
    /// All user-supplied values are HTML-encoded.
    /// </summary>
    public static class Email
    {
        public static EmailMessage AfterRegistration(string to, string firstName, string lastName, string activationLink)
        {
            var body =
                $"<h2>Twoje konto <b>{Enc(firstName)} {Enc(lastName)}</b> w aplikacji do głosowania zostało założone.</h2>" +
                "<p>Aby je aktywować, kliknij poniższy link:</p>" +
                $"<p><a href=\"{Enc(activationLink)}\">Aktywuj konto</a></p>" +
                "<p>Jeśli to nie Ty zakładałeś konto, zignoruj tę wiadomość.</p>";
            return new EmailMessage(to, "Aktywacja konta w aplikacji do głosowania", body);
        }

        public static EmailMessage VoteReceipt(string to, string electionName, string hash)
        {
            var body =
                $"<h2>Dziękujemy za oddanie głosu w wyborach: {Enc(electionName)}</h2>" +
                $"<p>Hash Twojego głosu: <b>{Enc(hash)}</b></p>" +
                "<p>Możesz sprawdzić, czy Twój głos znajduje się w łańcuchu, korzystając z wyszukiwarki głosów w aplikacji.</p>";
            return new EmailMessage(to, "Potwierdzenie oddania głosu", body);
        }

        public static EmailMessage PasswordChanged(string to)
        {
            const string body =
                "<h2>Twoje hasło zostało zmienione.</h2>" +
                "<p>Jeśli to nie Ty zmieniałeś hasło, natychmiast skontaktuj się z administratorem.</p>";
            return new EmailMessage(to, "Zmiana hasła", body);
        }

        public static EmailMessage PasswordReset(string to, string resetLink, TimeSpan validFor)
        {
            var minutes = (int)Math.Round(validFor.TotalMinutes);
            var body =
                "<h2>Reset hasła</h2>" +
                $"<p>Aby ustawić nowe hasło, kliknij poniższy link. Link jest ważny przez {minutes} minut.</p>" +
                $"<p><a href=\"{Enc(resetLink)}\">Ustaw nowe hasło</a></p>" +
                "<p>Jeśli nie prosiłeś o reset hasła, zignoruj tę wiadomość. Twoje hasło pozostaje bez zmian.</p>";
            return new EmailMessage(to, "Reset hasła w aplikacji do głosowania", body);
        }

        private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}

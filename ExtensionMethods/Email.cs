using InternetVotingApplication.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace InternetVotingApplication.ExtensionMethods
{
    public static class Email
    {
        private const string FromEmail = "aplikacjadoglosowania@gmail.com";
        private const string SmtpServer = "smtp.ethereal.email";
        private const int SmtpPort = 587;
        private const string SmtpUser = "kariane91@ethereal.email";
        private const string SmtpPass = "KwbBAKZ3Rbssk871tU";

        public static void SendEmailAfterRegistration(Uzytkownik user)
        {
            var body = $"<h2>Twoje konto <b>{user.Imie} {user.Nazwisko}</b> w aplikacji do głosowania zostało założone pomyślnie!</h2><br /><br />Naciśnij ten link aby aktywować konto<br /><a href='https://localhost:44342/Account/Activation/{user.KodAktywacyjny}'>Naciśnij aby aktywować konto.</a><br />";
            SendEmail(user.Email, "Link aktywacyjny do konta w aplikacji do głosowania", body);
        }

        public static void SendEmailVoteHash(GlosowanieWyborcze electionVoteDB, string userEmail)
        {
            var body = $"<h2>Hash twojego głosu: <b>{electionVoteDB.Hash}</b></h2></br> <p>Możesz sprawdzić poprawność swojego głosu w wyszukiwarce znajdującej się na stronie</p>";
            SendEmail(userEmail, "Dziękujemy za zagłosowanie w wyborach", body);
        }

        public static void SendEmailChangePassword(string userEmail)
        {
            var body = "<h2>Twoje hasło zostało zmienione!</h2></br> <p>Jeśli otrzymałeś tą wiadomość a to nie ty dokonałeś zmiany hasła skontaktuj się z administratorem.</p>";
            SendEmail(userEmail, "Pomyślna zmiana hasła!", body);
        }

        public static void SendNewPassword(string password, Uzytkownik user)
        {
            var body = $"<h2>Twoje hasło zostało zresetowane i zastąpione nowym.!</h2></br> <p>Nowe hasło: {password}</p></br><p>Pamiętaj aby po zalogowaniu się tym hasłem zmienić je na własne nowe!</p>";
            SendEmail(user.Email, "Przypomnienie hasła!", body);
        }

        private static void SendEmail(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            smtp.Connect(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
            smtp.Authenticate(SmtpUser, SmtpPass);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}
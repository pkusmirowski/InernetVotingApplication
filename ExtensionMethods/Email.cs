using InernetVotingApplication.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading.Tasks;

namespace InernetVotingApplication.ExtensionMethods
{
    public static class Email
    {
        public static async Task SendEmailAfterRegistrationAsync(Uzytkownik user)
        {
            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(user.Email));
            sendEmail.Subject = "Link aktywacyjny do konta w aplikacji do głosowania";
            sendEmail.Body = new TextPart(TextFormat.Html)
            {
                Text = $"<h2>Twoje konto <b>{user.Imie} {user.Nazwisko}</b> w aplikacji do głosowania zostało założone pomyślnie!</h2><br /><br />Naciśnij ten link aby aktywować konto<br /><a href='https://inernetvotingapplication.azurewebsites.net/Account/Activation/{user.KodAktywacyjny}'>Naciśnij aby aktywować konto.</a><br />"
            };

            await ConnectToSendAsync(sendEmail);
        }

        public static async Task SendEmailVoteHashAsync(GlosowanieWyborcze electionVoteDB, string userEmail)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new ArgumentException("Email address cannot be null or empty.", nameof(userEmail));
            }

            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(userEmail));
            sendEmail.Subject = "Dziękujemy za zagłosowanie w wyborach";
            sendEmail.Body = new TextPart(TextFormat.Html)
            {
                Text = $"<h2>Hash twojego głosu: <b>{electionVoteDB.Hash}</b></h2></br> <p>Możesz sprawdzić poprawność swojego głosu w wyszukiwarce znajdującej się na stronie</p>"
            };

            await ConnectToSendAsync(sendEmail);
        }

        public static async Task SendEmailChangePasswordAsync(string userEmail)
        {
            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(userEmail));
            sendEmail.Subject = "Pomyślna zmiana hasła!";
            sendEmail.Body = new TextPart(TextFormat.Html)
            {
                Text = "<h2>Twoje hasło zostało zmienione!</h2></br> <p>Jeśli otrzymałeś tę wiadomość, a to nie ty dokonałeś zmiany hasła, skontaktuj się z administratorem.</p>"
            };
            await ConnectToSendAsync(sendEmail);
        }

        public static async Task SendNewPasswordAsync(string password, Uzytkownik user)
        {
            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(user.Email));
            sendEmail.Subject = "Przypomnienie hasła!";
            sendEmail.Body = new TextPart(TextFormat.Html)
            {
                Text = $"<h2>Twoje hasło zostało zresetowane i zastąpione nowym.!</h2></br><p>Nowe hasło: {password}</p></br><p>Pamiętaj, aby po zalogowaniu się tym hasłem zmienić je na własne nowe!</p>"
            };
            await ConnectToSendAsync(sendEmail);
        }

        private static async Task ConnectToSendAsync(MimeMessage sendEmail)
        {
            // Ustawienia połączenia SMTP
            const string host = "smtp-relay.sendinblue.com";
            const int port = 587;
            const SecureSocketOptions options = SecureSocketOptions.StartTls;

            // Dane do autoryzacji
            const string fromEmail = "aplikacjadoglosowania@gmail.com";
            const string password = "smtp"; // upewnij się, że to jest poprawne hasło

            // Tworzenie klienta SMTP i wysyłanie wiadomości
            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync(host, port, options);
                await smtp.AuthenticateAsync(fromEmail, password);
                await smtp.SendAsync(sendEmail);
                await smtp.DisconnectAsync(true);
            }
        }

    }
}

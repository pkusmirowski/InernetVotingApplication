using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace InternetVotingApplication.Services
{
    public class EmailService(IConfiguration configuration) : IEmailService
    {
        private readonly IConfiguration _configuration = configuration;

        public void SendEmailAfterRegistration(Uzytkownik user)
        {
            var body = $"<h2>Twoje konto <b>{user.Imie} {user.Nazwisko}</b> w aplikacji do głosowania zostało założone pomyślnie!</h2><br /><br />Naciśnij ten link aby aktywować konto<br /><a href='https://localhost:44342/Account/Activation/{user.KodAktywacyjny}'>Naciśnij aby aktywować konto.</a><br />";
            SendEmail(user.Email, "Link aktywacyjny do konta w aplikacji do głosowania", body);
        }

        public void SendEmailVoteHash(GlosowanieWyborcze electionVoteDB, string userEmail)
        {
            var body = $"<h2>Hash twojego głosu: <b>{electionVoteDB.Hash}</b></h2></br> <p>Możesz sprawdzić poprawność swojego głosu w wyszukiwarce znajdującej się na stronie</p>";
            SendEmail(userEmail, "Dziękujemy za zagłosowanie w wyborach", body);
        }

        public void SendEmailChangePassword(string userEmail)
        {
            var body = "<h2>Twoje hasło zostało zmienione!</h2></br> <p>Jeśli otrzymałeś tą wiadomość a to nie ty dokonałeś zmiany hasła skontaktuj się z administratorem.</p>";
            SendEmail(userEmail, "Pomyślna zmiana hasła!", body);
        }

        public void SendNewPassword(string password, Uzytkownik user)
        {
            var body = $"<h2>Twoje hasło zostało zresetowane i zastąpione nowym.!</h2></br> <p>Nowe hasło: {password}</p></br><p>Pamiętaj aby po zalogowaniu się tym hasłem zmienić je na własne nowe!</p>";
            SendEmail(user.Email, "Przypomnienie hasła!", body);
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:FromEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            smtp.Connect(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);
            smtp.Authenticate(_configuration["EmailSettings:SmtpUser"], _configuration["EmailSettings:SmtpPass"]);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}

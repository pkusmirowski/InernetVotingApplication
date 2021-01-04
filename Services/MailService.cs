﻿using InernetVotingApplication.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
namespace InernetVotingApplication.Services
{
    public class MailService
    {
        public void SendEmailAfterRegistration(Uzytkownik user)
        {
            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(user.Email));
            sendEmail.Subject = "Pomyślne założenie konta w apllikacji do głosowania";
            sendEmail.Body = new TextPart(TextFormat.Html) { Text = "<h2>Twoje konto <b>" + user.Imie + " " + user.Nazwisko + "</b> w aplikacji do głosowania zostało założone pomyślnie!</h2>" };
            var smtp = new SmtpClient();
            smtp.Connect("smtp.ethereal.email", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("cleve.daugherty2@ethereal.email", "96712EmxRGP6ZNWA8V");
            smtp.Send(sendEmail);
            smtp.Disconnect(true);
        }

        public void SendEmailVoteHash(GlosowanieWyborcze electionVoteDB, string userEmail)
        {
            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(userEmail));
            sendEmail.Subject = "Dziękujemy za zagłosowanie w wyborach";
            sendEmail.Body = new TextPart(TextFormat.Html) { Text = "<h2>Hash twojego głosu: <b>" + electionVoteDB.Hash + "</b></h2></br> <p>Możesz sprawdzić poprawność swojego głosu w wyszukiwarce znajdującej się na stronie</p>" };
            var smtp = new SmtpClient();
            smtp.Connect("smtp.ethereal.email", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("cleve.daugherty2@ethereal.email", "96712EmxRGP6ZNWA8V");
            smtp.Send(sendEmail);
            smtp.Disconnect(true);
        }

        public void SendEmailChangePassword(string userEmail)
        {
            var sendEmail = new MimeMessage();
            sendEmail.From.Add(MailboxAddress.Parse("aplikacjadoglosowania@gmail.com"));
            sendEmail.To.Add(MailboxAddress.Parse(userEmail));
            sendEmail.Subject = "Pomyślna zmiana hasła!";
            sendEmail.Body = new TextPart(TextFormat.Html) { Text = "<h2>Twoje hasło zostało zmienione!</h2></br> <p>Jeśli otrzymałeś tą wiadomość a to nie ty dokonałeś zmiany hasła skontaktuj się z administratorem.</p>" };
            var smtp = new SmtpClient();
            smtp.Connect("smtp.ethereal.email", 587, SecureSocketOptions.StartTls);
            smtp.Authenticate("cleve.daugherty2@ethereal.email", "96712EmxRGP6ZNWA8V");
            smtp.Send(sendEmail);
            smtp.Disconnect(true);
        }
    }
}
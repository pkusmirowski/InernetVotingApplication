using InternetVotingApplication.Models;

namespace InternetVotingApplication.Interfaces
{
    public interface IEmailService
    {
        void SendEmailAfterRegistration(Uzytkownik user);
        void SendEmailVoteHash(GlosowanieWyborcze electionVoteDB, string userEmail);
        void SendEmailChangePassword(string userEmail);
        void SendNewPassword(string password, Uzytkownik user);
    }
}

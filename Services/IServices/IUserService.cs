using InernetVotingApplication.Models;
using System;
using System.Threading.Tasks;

namespace InernetVotingApplication.IServices
{
    public interface IUserService
    {
        Task<bool> RegisterAsync(Uzytkownik user);
        Task<int> LoginAsync(Logowanie user);

        public string GetLoggedEmail(Logowanie user);

        Task<bool> AuthenticateUser(Logowanie user);

        public bool ChangePassword(ChangePassword password, string userEmail);

        public bool GetUserByAcitvationCode(Guid activationCode);

        Task<bool> RecoverPassword(PasswordRecovery password);
    }
}

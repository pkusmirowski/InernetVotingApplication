using InternetVotingApplication.Models;
using System;
using System.Threading.Tasks;

namespace InternetVotingApplication.Interfaces
{
    public interface IUserService
    {
        Task<bool> RegisterAsync(Uzytkownik user);
        Task<int> LoginAsync(Logowanie user);
        Task<bool> AuthenticateUser(Logowanie user);
        bool ChangePassword(ChangePassword password, string userEmail);
        bool GetUserByAcitvationCode(Guid activationCode);
        Task<bool> RecoverPassword(PasswordRecovery password);
    }
}

using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace InternetVotingApplication.Services
{
    public class UserService(InternetVotingContext context) : IUserService
    {
        private readonly InternetVotingContext _context = context;

        public async Task<bool> RegisterAsync(Uzytkownik user)
        {
            if (await _context.Uzytkowniks.AnyAsync(x => x.Email == user.Email || x.Pesel == user.Pesel))
            {
                return false;
            }

            if (!PeselValidation.IsValidPESEL(user.Pesel) || !EmailValidation.IsValidEmail(user.Email))
            {
                return false;
            }

            user.KodAktywacyjny = Guid.NewGuid();
            user.Haslo = BC.HashPassword(user.Haslo);
            user.JestAktywne = 0;

            _context.Uzytkowniks.Add(user);
            await _context.SaveChangesAsync();

            Email.SendEmailAfterRegistration(user);
            return true;
        }

        public async Task<int> LoginAsync(Logowanie user)
        {
            var userAccount = await _context.Uzytkowniks
                .Where(u => u.Email == user.Email)
                .Select(u => new { u.Id, u.JestAktywne, u.Haslo })
                .FirstOrDefaultAsync();

            if (userAccount == null || userAccount.JestAktywne != 1 || !BC.Verify(user.Haslo, userAccount.Haslo))
            {
                return 2;
            }

            var isAdmin = await _context.Administrators.AnyAsync(a => a.IdUzytkownik == userAccount.Id);
            return isAdmin ? 0 : 1;
        }

        public async Task<bool> AuthenticateUser(Logowanie user)
        {
            var account = await _context.Uzytkowniks
                .Where(x => x.Email == user.Email)
                .Select(x => x.Haslo)
                .FirstOrDefaultAsync();

            return account != null && BC.Verify(user.Haslo, account);
        }

        public bool ChangePassword(ChangePassword password, string userEmail)
        {
            var account = _context.Uzytkowniks.SingleOrDefault(x => x.Email == userEmail);
            if (account == null || !BC.Verify(password.Password, account.Haslo) || password.NewPassword != password.ConfirmNewPassword)
            {
                return false;
            }

            account.Haslo = BC.HashPassword(password.NewPassword);
            _context.Update(account);
            _context.SaveChanges();

            Email.SendEmailChangePassword(userEmail);
            return true;
        }

        public bool GetUserByAcitvationCode(Guid activationCode)
        {
            var user = _context.Uzytkowniks.SingleOrDefault(x => x.KodAktywacyjny == activationCode);
            if (user == null)
            {
                return false;
            }

            user.JestAktywne = 1;
            user.KodAktywacyjny = Guid.Empty;

            _context.Update(user);
            _context.SaveChanges();

            return true;
        }

        public async Task<bool> RecoverPassword(PasswordRecovery password)
        {
            var user = await _context.Uzytkowniks
                .SingleOrDefaultAsync(x => x.Pesel == password.Pesel && x.Email == password.Email);

            if (user == null)
            {
                return false;
            }

            string newPassword = GeneratePassword.CreateRandomPassword(8);
            user.Haslo = BC.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            Email.SendNewPassword(newPassword, user);
            return true;
        }
    }
}

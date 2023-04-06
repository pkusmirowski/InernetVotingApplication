using InernetVotingApplication.ExtensionMethods;
using InernetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace InernetVotingApplication.Services
{
    public class UserService
    {
        private readonly InternetVotingContext _context;

        public UserService(InternetVotingContext context)
        {
            _context = context;
        }

        public async Task<bool> RegisterAsync(Uzytkownik user)
        {
            var existingUser = await _context.Uzytkowniks.FirstOrDefaultAsync(x => x.Email == user.Email || x.Pesel == user.Pesel);
            if (existingUser != null)
            {
                return false;
            }

            if (!PeselValidation.IsValidPESEL(user.Pesel) || !EmailValidation.IsValidEmail(user.Email))
            {
                return false;
            }

            user.KodAktywacyjny = Guid.NewGuid();
            user.Haslo = await Task.Run(() => BC.HashPassword(user.Haslo));
            user.JestAktywne = false;
            await _context.AddAsync(user);
            await _context.SaveChangesAsync();

            await Email.SendEmailAfterRegistrationAsync(user);
            return true;
        }

        //Zwraca
        //0 - gdy user jest Adminem
        //1 - gdu user jest Userem
        //2 - gdy nie jest ani Adminem ani Userem
        public async Task<int> LoginAsync(Logowanie user)
        {
            var userAccount = await _context.Uzytkowniks
                .FirstOrDefaultAsync(u => u.Email == user.Email && u.Haslo == user.Haslo);

            if (userAccount == null || !userAccount.JestAktywne)
            {
                return 2;
            }

            var checkIfAdmin = await _context.Administrators
                .AnyAsync(a => a.IdUzytkownik == userAccount.Id);

            return checkIfAdmin ? 0 : 1;
        }


        // Zwraca wartość true, jeśli użytkownik o podanym adresie email istnieje i podane hasło jest poprawne, w przeciwnym przypadku zwraca wartość false
        public async Task<bool> AuthenticateUser(Logowanie login)
        {
            var user = await _context.Uzytkowniks
                .FirstOrDefaultAsync(x => x.Email == login.Email);

            return user != null && BC.Verify(login.Haslo, user.Haslo);
        }

        public async Task<bool> ChangePasswordAsync(ChangePassword password, string userEmail)
        {
            var account = await _context.Uzytkowniks.FirstOrDefaultAsync(x => x.Email == userEmail);

            if (account == null)
            {
                return false;
            }

            var verifyPassword = BC.Verify(password.Password, account.Haslo);

            if (password.NewPassword != password.ConfirmNewPassword || !verifyPassword)
            {
                return false;
            }

            account.Haslo = await Task.Run(() => BC.HashPassword(password.NewPassword));
            _context.Uzytkowniks.Update(account);
            await _context.SaveChangesAsync();

            await Email.SendEmailChangePasswordAsync(userEmail);

            return true;
        }

        public async Task<bool> GetUserByActivationCodeAsync(Guid activationCode)
        {
            var user = await _context.Uzytkowniks.FirstOrDefaultAsync(x => x.KodAktywacyjny == activationCode);

            if (user == null)
            {
                return false;
            }

            user.JestAktywne = true;
            user.KodAktywacyjny = Guid.Empty;
            _context.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> RecoverPassword(PasswordRecovery password)
        {
            var user = await _context.Uzytkowniks
                .FirstOrDefaultAsync(x => x.Pesel == password.Pesel && x.Email == password.Email);

            if (user == null)
            {
                return false;
            }

            string newPassword = GeneratePassword.CreateRandomPassword(8).ToString();
            user.Haslo = BC.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            await Email.SendNewPasswordAsync(newPassword, user);

            return true;
        }

    }
}

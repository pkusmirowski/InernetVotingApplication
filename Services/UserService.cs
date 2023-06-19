using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace InternetVotingApplication.Services
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
            var isExistingEmail = await _context.Uzytkowniks.AnyAsync(x => x.Email == user.Email);
            var isExistingPesel = await _context.Uzytkowniks.AnyAsync(x => x.Pesel == user.Pesel);

            if (isExistingEmail || isExistingPesel)
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

        //Zwraca
        //0 - gdy user jest Adminem
        //1 - gdu user jest Userem
        //2 - gdy nie jest ani Adminem ani Userem
        public async Task<int> LoginAsync(Logowanie user)
        {
            var queryActive = _context.Uzytkowniks.Where(u => u.Email == user.Email).Select(u => u.JestAktywne == 1);
            var getUserId = await _context.Uzytkowniks.Where(u => u.Email == user.Email).Select(u => u.Id).FirstOrDefaultAsync();
            var getUserStatus = await _context.Uzytkowniks.Where(u => u.JestAktywne == 1 && u.Email == user.Email).Select(u => u.JestAktywne).FirstOrDefaultAsync();
            var checkIfAdmin = await _context.Administrators.AnyAsync(a => a.IdUzytkownik == getUserId);

            if (getUserStatus == 1 && !string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(user.Haslo) && await queryActive.FirstOrDefaultAsync() && await AuthenticateUser(user))
            {
                if (checkIfAdmin)
                {
                    return 0;
                }
                return 1;
            }

            return 2;
        }

        public async Task<bool> AuthenticateUser(Logowanie user)
        {
            var account = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Email == user.Email);

            return BC.Verify(user.Haslo, account.Haslo);
        }

        public bool ChangePassword(ChangePassword password, string userEmail)
        {
            var account = _context.Uzytkowniks.SingleOrDefault(x => x.Email == userEmail);
            var verifyPassword = BC.Verify(password.Password, account.Haslo);

            if (password.NewPassword != password.ConfirmNewPassword)
            {
                return false;
            }

            if (!verifyPassword)
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
            var user = _context.Uzytkowniks.FirstOrDefault(x => x.KodAktywacyjny == activationCode);

            if (user != null)
            {
                user.JestAktywne = 1;
                user.KodAktywacyjny = Guid.Empty;

                _context.Update(user);
                _context.SaveChanges();

                return true;
            }

            return false;
        }

        public async Task<bool> RecoverPassword(PasswordRecovery password)
        {
            var user = await _context.Uzytkowniks.FirstOrDefaultAsync(x => x.Pesel == password.Pesel && x.Email == password.Email);

            if (user != null)
            {
                string newPassword = GeneratePassword.CreateRandomPassword(8);
                user.Haslo = BC.HashPassword(newPassword);
                await _context.SaveChangesAsync();
                Email.SendNewPassword(newPassword, user);
                return true;
            }

            return false;
        }
    }
}

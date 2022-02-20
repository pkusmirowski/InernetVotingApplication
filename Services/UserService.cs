using InernetVotingApplication.ExtensionMethods;
using InernetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace InernetVotingApplication.Services
{
    public class UserService
    {
        private readonly InternetVotingContext _context;
        private readonly MailService _mailService;
        public UserService(InternetVotingContext context, MailService mailService)
        {
            _context = context;
            _mailService = mailService;
        }

        public async Task<bool> Register(Uzytkownik user)
        {
            var email = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Email == user.Email);
            var pesel = await _context.Uzytkowniks.SingleOrDefaultAsync(x => x.Pesel == user.Pesel);

            if (email != null && email.Email == user.Email)
            {
                return false;
            }

            if (pesel != null && pesel.Pesel == user.Pesel)
            {
                return false;
            }

            if (!PeselValidation.IsValidPESEL(user.Pesel) || !EmailValidation.IsValidEmail(user.Email))
            {
                return false;
            }
            Guid activationCode = Guid.NewGuid();
            user.KodAktywacyjny = activationCode;
            user.Haslo = BC.HashPassword(user.Haslo);
            user.JestAktywne = false;
            await _context.AddAsync(user);
            _context.SaveChanges();

            _mailService.SendEmailAfterRegistration(user);
            return true;
        }

        //Zwraca
        //0 - gdy user jest Adminem
        //1 - gdu user jest Userem
        //2 - gdy nie jest ani Adminem ani Userem
        public async Task<int> LoginAsync(Logowanie user)
        {
            var queryActive = from Uzytkownik in _context.Uzytkowniks
                              where Uzytkownik.Email == user.Email
                              select Uzytkownik.JestAktywne;

            var getUserId = await (from Uzytkownik in _context.Uzytkowniks
                                   where Uzytkownik.Email == user.Email
                                   select Uzytkownik.Id).FirstOrDefaultAsync();

            var getUserStatus = await (from Uzytkownik in _context.Uzytkowniks
                                       where Uzytkownik.JestAktywne
                                       select Uzytkownik.JestAktywne).FirstOrDefaultAsync();

            var checkIfAdmin = await (from Administrator in _context.Administrators
                                      where Administrator.IdUzytkownik == getUserId
                                      select Administrator.IdUzytkownik).FirstOrDefaultAsync();

            if (getUserStatus && user.Email != null && user.Haslo != null && (await queryActive.FirstOrDefaultAsync()) && await AuthenticateUser(user))
            {
                if (getUserId == checkIfAdmin)
                {
                    return 0;
                }
                return 1;
            }

            return 2;
        }

        public string GetLoggedEmail(Logowanie user)
        {
            var queryName = from Uzytkownik in _context.Uzytkowniks
                            where Uzytkownik.Email == user.Email
                            select Uzytkownik.Email;

            return queryName.ToString();
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

            _mailService.SendEmailChangePassword(userEmail);

            return true;
        }

        public bool GetUserByAcitvationCode(Guid activationCode)
        {
            var user = (from Uzytkownik in _context.Uzytkowniks
                        where Uzytkownik.KodAktywacyjny == activationCode
                        select Uzytkownik).FirstOrDefault();
            if (user != null)
            {
                user.JestAktywne = true;
                user.KodAktywacyjny = Guid.Empty;
                _context.Update(user);
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<bool> RecoverPassword(PasswordRecovery password)
        {
            var userByPesel = await (from Uzytkownik in _context.Uzytkowniks
                                     where Uzytkownik.Pesel == password.Pesel
                                     select Uzytkownik).FirstOrDefaultAsync();

            var userByEmail = await (from Uzytkownik in _context.Uzytkowniks
                                     where Uzytkownik.Email == password.Email
                                     select Uzytkownik).FirstOrDefaultAsync();

            if (userByPesel != null && userByEmail != null && userByPesel.Email == userByEmail.Email && userByPesel.Pesel == userByEmail.Pesel)
            {
                string newPassword = GeneratePassword.CreateRandomPassword(8);
                userByPesel.Haslo = BC.HashPassword(newPassword);
                await _context.SaveChangesAsync();
                _mailService.SendNewPassword(newPassword, userByPesel);
                return true;
            }

            return false;
        }
    }
}
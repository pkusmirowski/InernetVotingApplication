using InternetVotingApplication.Configuration;
using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BC = BCrypt.Net.BCrypt;

namespace InternetVotingApplication.Services
{
    public class UserService(
        InternetVotingContext context,
        IEmailSender emailSender,
        IOptions<SecurityOptions> securityOptions,
        TimeProvider timeProvider,
        ILogger<UserService> logger) : IUserService
    {
        /// <summary>Hash verified when the account does not exist, so that response time does not reveal account existence.</summary>
        private static readonly string DummyHash = BC.HashPassword("dummy-password-for-timing");

        private readonly SecurityOptions _security = securityOptions.Value;

        public async Task<RegistrationStatus> RegisterAsync(RegisterViewModel model, Func<Guid, string> activationLinkFactory)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(activationLinkFactory);

            var email = NormalizeEmail(model.Email);
            if (!EmailValidation.IsValidEmail(email))
            {
                return RegistrationStatus.InvalidEmail;
            }

            if (!PeselValidation.IsValidPESEL(model.Pesel))
            {
                return RegistrationStatus.InvalidPesel;
            }

            if (await context.Uzytkowniks.AnyAsync(u => u.Email == email))
            {
                return RegistrationStatus.EmailTaken;
            }

            if (await context.Uzytkowniks.AnyAsync(u => u.Pesel == model.Pesel))
            {
                return RegistrationStatus.PeselTaken;
            }

            var user = new Uzytkownik
            {
                Imie = model.Imie.Trim(),
                Nazwisko = model.Nazwisko.Trim(),
                Pesel = model.Pesel,
                Email = email,
                DataUrodzenia = model.DataUrodzenia!.Value.Date,
                Haslo = BC.HashPassword(model.Haslo),
                JestAktywne = false,
                KodAktywacyjny = Guid.NewGuid(),
                DataRejestracji = Now(),
            };

            context.Uzytkowniks.Add(user);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Unique index violated by a concurrent registration.
                logger.LogWarning(ex, "Registration for {Email} rejected by a unique constraint", email);
                return RegistrationStatus.EmailTaken;
            }

            await emailSender.SendAsync(Email.AfterRegistration(user.Email, user.Imie, user.Nazwisko, activationLinkFactory(user.KodAktywacyjny.Value)));
            logger.LogInformation("User {UserId} registered", user.Id);
            return RegistrationStatus.Success;
        }

        public async Task<LoginOutcome> LoginAsync(Logowanie model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var email = NormalizeEmail(model.Email);
            var user = await context.Uzytkowniks.SingleOrDefaultAsync(u => u.Email == email);
            var now = Now();

            if (user == null)
            {
                BC.Verify(model.Haslo, DummyHash);
                return new LoginOutcome(LoginStatus.InvalidCredentials, null, false);
            }

            if (user.ZablokowaneDo.HasValue && user.ZablokowaneDo > now)
            {
                return new LoginOutcome(LoginStatus.LockedOut, null, false);
            }

            if (!BC.Verify(model.Haslo, user.Haslo))
            {
                user.NieudaneLogowania++;
                var lockedNow = false;
                if (user.NieudaneLogowania >= _security.MaxFailedLoginAttempts)
                {
                    user.ZablokowaneDo = now.Add(_security.LockoutDuration);
                    user.NieudaneLogowania = 0;
                    lockedNow = true;
                    logger.LogWarning("User {UserId} locked out until {Until}", user.Id, user.ZablokowaneDo);
                }

                await context.SaveChangesAsync();
                return new LoginOutcome(lockedNow ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials, null, false);
            }

            if (!user.JestAktywne)
            {
                return new LoginOutcome(LoginStatus.NotActivated, null, false);
            }

            if (user.NieudaneLogowania != 0 || user.ZablokowaneDo.HasValue)
            {
                user.NieudaneLogowania = 0;
                user.ZablokowaneDo = null;
                await context.SaveChangesAsync();
            }

            var isAdmin = await context.Administrators.AnyAsync(a => a.IdUzytkownik == user.Id);
            return new LoginOutcome(LoginStatus.Success, user, isAdmin);
        }

        public async Task<bool> ActivateAsync(Guid activationCode)
        {
            if (activationCode == Guid.Empty)
            {
                return false;
            }

            var user = await context.Uzytkowniks.SingleOrDefaultAsync(u => u.KodAktywacyjny == activationCode);
            if (user == null)
            {
                return false;
            }

            user.JestAktywne = true;
            user.KodAktywacyjny = null;
            await context.SaveChangesAsync();
            logger.LogInformation("User {UserId} activated", user.Id);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePassword model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var user = await context.Uzytkowniks.FindAsync(userId);
            if (user == null || !BC.Verify(model.Password, user.Haslo))
            {
                return false;
            }

            user.Haslo = BC.HashPassword(model.NewPassword);
            await context.SaveChangesAsync();
            await emailSender.SendAsync(Email.PasswordChanged(user.Email));
            return true;
        }

        public async Task RequestPasswordResetAsync(string email, Func<Guid, string> resetLinkFactory)
        {
            ArgumentNullException.ThrowIfNull(resetLinkFactory);

            var normalized = NormalizeEmail(email);
            var user = await context.Uzytkowniks.SingleOrDefaultAsync(u => u.Email == normalized && u.JestAktywne);
            if (user == null)
            {
                logger.LogInformation("Password reset requested for unknown or inactive account");
                return;
            }

            user.TokenResetuHasla = Guid.NewGuid();
            user.TokenResetuWygasa = Now().Add(_security.PasswordResetTokenLifetime);
            await context.SaveChangesAsync();

            await emailSender.SendAsync(Email.PasswordReset(user.Email, resetLinkFactory(user.TokenResetuHasla.Value), _security.PasswordResetTokenLifetime));
        }

        public async Task<bool> IsPasswordResetTokenValidAsync(Guid token)
        {
            if (token == Guid.Empty)
            {
                return false;
            }

            var now = Now();
            return await context.Uzytkowniks.AnyAsync(u => u.TokenResetuHasla == token && u.TokenResetuWygasa > now);
        }

        public async Task<bool> ResetPasswordAsync(Guid token, string newPassword)
        {
            ArgumentException.ThrowIfNullOrEmpty(newPassword);
            if (token == Guid.Empty)
            {
                return false;
            }

            var now = Now();
            var user = await context.Uzytkowniks.SingleOrDefaultAsync(u => u.TokenResetuHasla == token && u.TokenResetuWygasa > now);
            if (user == null)
            {
                return false;
            }

            user.Haslo = BC.HashPassword(newPassword);
            user.TokenResetuHasla = null;
            user.TokenResetuWygasa = null;
            user.NieudaneLogowania = 0;
            user.ZablokowaneDo = null;
            await context.SaveChangesAsync();

            await emailSender.SendAsync(Email.PasswordChanged(user.Email));
            logger.LogInformation("Password reset completed for user {UserId}", user.Id);
            return true;
        }

        private DateTime Now() => timeProvider.GetLocalNow().DateTime;

        private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}

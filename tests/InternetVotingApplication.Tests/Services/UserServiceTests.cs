using InternetVotingApplication.Configuration;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace InternetVotingApplication.Tests.Services
{
    public sealed class UserServiceTests : IDisposable
    {
        private readonly SqliteDatabase _db = new();
        private readonly FakeEmailSender _email = new();
        private readonly FakeTimeProvider _clock = TestData.Clock();
        private readonly SecurityOptions _security = new() { MaxFailedLoginAttempts = 3, LockoutDuration = TimeSpan.FromMinutes(15), PasswordResetTokenLifetime = TimeSpan.FromHours(1) };

        private UserService CreateService(InternetVotingContext context)
        {
            return new UserService(context, _email, Options.Create(_security), _clock, TestData.Logger<UserService>());
        }

        private static RegisterViewModel ValidRegistration(string email = "anna@example.com", string pesel = "02070803628")
        {
            return new RegisterViewModel
            {
                Imie = "Anna",
                Nazwisko = "Nowak",
                Email = email,
                Pesel = pesel,
                DataUrodzenia = new DateTime(1995, 5, 5),
                Haslo = "Secret#Pass1",
                ConfirmPassword = "Secret#Pass1",
            };
        }

        [Fact]
        public async Task Register_creates_inactive_account_and_sends_activation_link()
        {
            using var context = _db.CreateContext();
            var service = CreateService(context);

            var status = await service.RegisterAsync(ValidRegistration("Anna@Example.com "), code => $"https://app/activate/{code}");

            Assert.Equal(RegistrationStatus.Success, status);
            var user = Assert.Single(context.Uzytkowniks);
            Assert.Equal("anna@example.com", user.Email);
            Assert.False(user.JestAktywne);
            Assert.NotNull(user.KodAktywacyjny);
            Assert.NotEqual("Secret#Pass1", user.Haslo);
            Assert.True(BCrypt.Net.BCrypt.Verify("Secret#Pass1", user.Haslo));

            var mail = Assert.Single(_email.Sent);
            Assert.Equal("anna@example.com", mail.To);
            Assert.Contains($"https://app/activate/{user.KodAktywacyjny}", mail.HtmlBody, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Register_rejects_duplicate_email_and_pesel_and_invalid_pesel()
        {
            using var context = _db.CreateContext();
            var service = CreateService(context);
            await service.RegisterAsync(ValidRegistration(), _ => "x");

            Assert.Equal(RegistrationStatus.EmailTaken, await service.RegisterAsync(ValidRegistration(pesel: "44051401359"), _ => "x"));
            Assert.Equal(RegistrationStatus.PeselTaken, await service.RegisterAsync(ValidRegistration(email: "other@example.com"), _ => "x"));
            Assert.Equal(RegistrationStatus.InvalidPesel, await service.RegisterAsync(ValidRegistration(email: "other@example.com", pesel: "12345678901"), _ => "x"));
            Assert.Equal(1, context.Uzytkowniks.Count());
        }

        [Fact]
        public async Task Activation_enables_account_once()
        {
            using var context = _db.CreateContext();
            var user = TestData.User(active: false);
            context.Uzytkowniks.Add(user);
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var code = user.KodAktywacyjny!.Value;

            Assert.True(await service.ActivateAsync(code));
            Assert.True(user.JestAktywne);
            Assert.Null(user.KodAktywacyjny);
            Assert.False(await service.ActivateAsync(code));
            Assert.False(await service.ActivateAsync(Guid.Empty));
        }

        [Fact]
        public async Task Login_succeeds_for_active_user_and_reports_admin_role()
        {
            using var context = _db.CreateContext();
            var user = TestData.User();
            context.Uzytkowniks.Add(user);
            context.Administrators.Add(new Administrator { IdUzytkownikNavigation = user });
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var outcome = await service.LoginAsync(new Logowanie { Email = "JAN@example.com", Haslo = "Correct#Horse1" });

            Assert.Equal(LoginStatus.Success, outcome.Status);
            Assert.Equal(user.Id, outcome.User!.Id);
            Assert.True(outcome.IsAdmin);
        }

        [Fact]
        public async Task Login_fails_for_wrong_password_unknown_user_and_inactive_account()
        {
            using var context = _db.CreateContext();
            context.Uzytkowniks.Add(TestData.User());
            context.Uzytkowniks.Add(TestData.User(email: "inactive@example.com", pesel: "02070803628", active: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            Assert.Equal(LoginStatus.InvalidCredentials, (await service.LoginAsync(new Logowanie { Email = "jan@example.com", Haslo = "wrong" })).Status);
            Assert.Equal(LoginStatus.InvalidCredentials, (await service.LoginAsync(new Logowanie { Email = "nobody@example.com", Haslo = "Correct#Horse1" })).Status);
            Assert.Equal(LoginStatus.NotActivated, (await service.LoginAsync(new Logowanie { Email = "inactive@example.com", Haslo = "Correct#Horse1" })).Status);
        }

        [Fact]
        public async Task Too_many_failed_logins_lock_the_account_until_lockout_expires()
        {
            using var context = _db.CreateContext();
            context.Uzytkowniks.Add(TestData.User());
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var wrong = new Logowanie { Email = "jan@example.com", Haslo = "wrong" };
            var right = new Logowanie { Email = "jan@example.com", Haslo = "Correct#Horse1" };

            Assert.Equal(LoginStatus.InvalidCredentials, (await service.LoginAsync(wrong)).Status);
            Assert.Equal(LoginStatus.InvalidCredentials, (await service.LoginAsync(wrong)).Status);
            Assert.Equal(LoginStatus.LockedOut, (await service.LoginAsync(wrong)).Status);
            Assert.Equal(LoginStatus.LockedOut, (await service.LoginAsync(right)).Status);

            _clock.Advance(TimeSpan.FromMinutes(16));

            Assert.Equal(LoginStatus.Success, (await service.LoginAsync(right)).Status);
        }

        [Fact]
        public async Task Password_reset_flow_replaces_password_and_invalidates_token()
        {
            using var context = _db.CreateContext();
            context.Uzytkowniks.Add(TestData.User());
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.RequestPasswordResetAsync("jan@example.com", token => $"https://app/reset/{token}");
            await service.RequestPasswordResetAsync("unknown@example.com", token => $"https://app/reset/{token}");

            var mail = Assert.Single(_email.Sent);
            var token = context.Uzytkowniks.Single().TokenResetuHasla!.Value;
            Assert.Contains(token.ToString(), mail.HtmlBody, StringComparison.Ordinal);

            Assert.True(await service.IsPasswordResetTokenValidAsync(token));
            Assert.True(await service.ResetPasswordAsync(token, "Brand#New1"));
            Assert.False(await service.IsPasswordResetTokenValidAsync(token));
            Assert.False(await service.ResetPasswordAsync(token, "Again#New1"));

            Assert.Equal(LoginStatus.Success, (await service.LoginAsync(new Logowanie { Email = "jan@example.com", Haslo = "Brand#New1" })).Status);
        }

        [Fact]
        public async Task Expired_reset_token_is_rejected()
        {
            using var context = _db.CreateContext();
            context.Uzytkowniks.Add(TestData.User());
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.RequestPasswordResetAsync("jan@example.com", _ => "x");
            var token = context.Uzytkowniks.Single().TokenResetuHasla!.Value;
            _clock.Advance(TimeSpan.FromHours(2));

            Assert.False(await service.IsPasswordResetTokenValidAsync(token));
            Assert.False(await service.ResetPasswordAsync(token, "Brand#New1"));
        }

        [Fact]
        public async Task Change_password_requires_current_password()
        {
            using var context = _db.CreateContext();
            var user = TestData.User();
            context.Uzytkowniks.Add(user);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            Assert.False(await service.ChangePasswordAsync(user.Id, new ChangePassword { Password = "wrong", NewPassword = "Brand#New1", ConfirmNewPassword = "Brand#New1" }));
            Assert.True(await service.ChangePasswordAsync(user.Id, new ChangePassword { Password = "Correct#Horse1", NewPassword = "Brand#New1", ConfirmNewPassword = "Brand#New1" }));
            Assert.True(BCrypt.Net.BCrypt.Verify("Brand#New1", user.Haslo));
            Assert.Single(_email.Sent);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}

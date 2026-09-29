using InternetVotingApplication.Configuration;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services;
using InternetVotingApplication.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace InternetVotingApplication.Tests.Services;

public sealed class UserServiceTests : IDisposable
{
    private readonly SqliteDatabase _db = new();
    private readonly FakeEmailSender _email = new();
    private readonly FakeTimeProvider _clock = TestData.Clock();
    private readonly SecurityOptions _security = new() { MaxFailedLoginAttempts = 3, LockoutDuration = TimeSpan.FromMinutes(15), PasswordResetTokenLifetime = TimeSpan.FromHours(1) };

    private readonly SeedingOptions _seeding = new();

    private UserService CreateService(InternetVotingContext context)
    {
        return new UserService(context, _email, Options.Create(_security), Options.Create(_seeding), TestData.Audit(context, _clock), _clock, TestData.Logger<UserService>());
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
    public async Task First_activated_user_becomes_admin_only_when_enabled_and_no_admin_exists()
    {
        using var context = _db.CreateContext();
        var first = TestData.User(email: "first@example.com", active: false);
        var second = TestData.User(email: "second@example.com", pesel: "02070803628", active: false);
        context.Uzytkowniks.AddRange(first, second);
        await context.SaveChangesAsync();

        var disabled = CreateService(context);
        Assert.True(await disabled.ActivateAsync(first.KodAktywacyjny!.Value));
        Assert.Empty(context.Administrators);

        _seeding.FirstActivatedUserIsAdmin = true;
        var enabled = CreateService(context);
        Assert.True(await enabled.ActivateAsync(second.KodAktywacyjny!.Value));
        var admin = Assert.Single(context.Administrators);
        Assert.Equal(second.Id, admin.IdUzytkownik);
        Assert.Contains(context.DziennikAudytu, a => a.Akcja == "AdminPromoted" && a.IdUzytkownik == second.Id);

        var third = TestData.User(email: "third@example.com", pesel: "00000000000", active: false);
        context.Uzytkowniks.Add(third);
        await context.SaveChangesAsync();
        Assert.True(await enabled.ActivateAsync(third.KodAktywacyjny!.Value));
        Assert.Single(context.Administrators);
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

        var token = Guid.Empty;
        await service.RequestPasswordResetAsync("jan@example.com", t => $"https://app/reset/{token = t}");
        await service.RequestPasswordResetAsync("unknown@example.com", t => $"https://app/reset/{t}");

        var mail = Assert.Single(_email.Sent);
        Assert.Contains(token.ToString(), mail.HtmlBody, StringComparison.Ordinal);

        // Only a hash of the token is stored; the value from the database does not work as a link.
        var stored = context.Uzytkowniks.Single().TokenResetuHasla!.Value;
        Assert.NotEqual(token, stored);
        Assert.Equal(UserService.HashToken(token), stored);
        Assert.False(await service.IsPasswordResetTokenValidAsync(stored));

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

        var token = Guid.Empty;
        await service.RequestPasswordResetAsync("jan@example.com", t => (token = t).ToString());
        Assert.True(await service.IsPasswordResetTokenValidAsync(token));
        _clock.Advance(TimeSpan.FromHours(2));

        Assert.False(await service.IsPasswordResetTokenValidAsync(token));
        Assert.False(await service.ResetPasswordAsync(token, "Brand#New1"));
    }

    [Fact]
    public async Task Session_state_follows_role_activation_and_password_changes()
    {
        using var context = _db.CreateContext();
        var user = TestData.User();
        context.Uzytkowniks.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var before = await service.GetSessionStateAsync(user.Id);
        Assert.NotNull(before);
        Assert.True(before.IsActive);
        Assert.False(before.IsAdmin);
        Assert.Equal(UserService.PasswordStamp(user.Haslo), before.PasswordStamp);

        context.Administrators.Add(new Administrator { IdUzytkownik = user.Id });
        await context.SaveChangesAsync();
        Assert.True((await service.GetSessionStateAsync(user.Id))!.IsAdmin);

        Assert.True(await service.ChangePasswordAsync(user.Id, new ChangePassword { Password = "Correct#Horse1", NewPassword = "Brand#New1", ConfirmNewPassword = "Brand#New1" }));
        Assert.NotEqual(before.PasswordStamp, (await service.GetSessionStateAsync(user.Id))!.PasswordStamp);

        Assert.Null(await service.GetSessionStateAsync(999));
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

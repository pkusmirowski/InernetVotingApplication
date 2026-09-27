using System.Security.Claims;
using InternetVotingApplication.Controllers;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using static InternetVotingApplication.Tests.Controllers.ControllerTestHelper;

namespace InternetVotingApplication.Tests.Controllers;

public class AccountControllerTests
{
    private readonly IUserService _users = Substitute.For<IUserService>();
    private readonly IResultsService _results = Substitute.For<IResultsService>();
    private readonly IAuthenticationService _authentication = Substitute.For<IAuthenticationService>();

    private AccountController Create(ClaimsPrincipal? user = null)
        => new AccountController(_users, _results, TestData.Logger<AccountController>()).Prepare(user, _authentication);

    private static RegisterViewModel Registration() => new()
    {
        Imie = "Anna",
        Nazwisko = "Nowak",
        Email = "anna@example.com",
        Pesel = "02070803628",
        DataUrodzenia = new DateTime(1990, 1, 1),
        Haslo = "Secret#Pass1",
        ConfirmPassword = "Secret#Pass1",
    };

    [Fact]
    public void Register_get_redirects_signed_in_users_to_dashboard()
    {
        var result = Create(Voter()).Register();

        AssertRedirect(result, "Dashboard", "Election");
    }

    [Fact]
    public void Register_get_shows_empty_form_for_anonymous()
    {
        var view = AssertView(Create().Register());

        Assert.IsType<RegisterViewModel>(view.Model);
    }

    [Fact]
    public async Task Register_post_with_invalid_model_redisplays_form_without_calling_service()
    {
        var controller = Create();
        controller.ModelState.AddModelError("Email", "x");

        var result = await controller.Register(Registration());

        AssertView(result);
        await _users.DidNotReceiveWithAnyArgs().RegisterAsync(default!, default!);
    }

    [Fact]
    public async Task Register_post_success_shows_confirmation_and_builds_absolute_activation_link()
    {
        Func<Guid, string>? factory = null;
        _users.RegisterAsync(Arg.Any<RegisterViewModel>(), Arg.Do<Func<Guid, string>>(f => factory = f)).Returns(RegistrationStatus.Success);

        var result = await Create().Register(Registration());

        AssertView(result, "RegisterConfirmation");
        Assert.NotNull(factory);
        Assert.Equal("https://app/Account/Activation", factory(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(RegistrationStatus.EmailTaken, "Email")]
    [InlineData(RegistrationStatus.PeselTaken, "Pesel")]
    [InlineData(RegistrationStatus.InvalidPesel, "Pesel")]
    [InlineData(RegistrationStatus.InvalidEmail, "Email")]
    public async Task Register_post_failure_adds_model_error_to_the_right_field(RegistrationStatus status, string field)
    {
        _users.RegisterAsync(Arg.Any<RegisterViewModel>(), Arg.Any<Func<Guid, string>>()).Returns(status);
        var controller = Create();

        var result = await controller.Register(Registration());

        AssertView(result);
        Assert.True(controller.ModelState[field]!.Errors.Count > 0);
    }

    [Fact]
    public void Login_get_redirects_authenticated_admin_to_panel_and_voter_to_dashboard()
    {
        AssertRedirect(Create(Voter(admin: true)).Login(), "Panel", "Admin");
        AssertRedirect(Create(Voter()).Login(), "Dashboard", "Election");
    }

    [Fact]
    public void Login_get_honours_local_return_url_only()
    {
        Assert.IsType<LocalRedirectResult>(Create(Voter()).Login("/Election/Voting/3"));
        AssertRedirect(Create(Voter()).Login("https://evil.example.com/"), "Dashboard", "Election");
    }

    [Fact]
    public async Task Login_post_success_signs_in_with_role_and_redirects()
    {
        var user = new Uzytkownik { Id = 7, Email = "a@b.pl", Imie = "A", Nazwisko = "B", Pesel = "x", Haslo = "h" };
        _users.LoginAsync(Arg.Any<Logowanie>()).Returns(new LoginOutcome(LoginStatus.Success, user, IsAdmin: true));

        var result = await Create().Login(new Logowanie { Email = "a@b.pl", Haslo = "x" });

        AssertRedirect(result, "Panel", "Admin");
        await _authentication.Received(1).SignInAsync(
            Arg.Any<HttpContext>(),
            "Cookies",
            Arg.Is<ClaimsPrincipal>(p => p.IsInRole(Roles.Admin) && p.FindFirstValue(ClaimTypes.NameIdentifier) == "7"),
            Arg.Any<AuthenticationProperties>());
    }

    [Theory]
    [InlineData(LoginStatus.InvalidCredentials, "Nieprawidłowy")]
    [InlineData(LoginStatus.NotActivated, "aktywowane")]
    [InlineData(LoginStatus.LockedOut, "zablokowane")]
    public async Task Login_post_failure_shows_message_and_does_not_sign_in(LoginStatus status, string fragment)
    {
        _users.LoginAsync(Arg.Any<Logowanie>()).Returns(new LoginOutcome(status, null, false));
        var controller = Create();

        var result = await controller.Login(new Logowanie { Email = "a@b.pl", Haslo = "x" });

        AssertView(result);
        var error = Assert.Single(controller.ModelState[string.Empty]!.Errors);
        Assert.Contains(fragment, error.ErrorMessage, StringComparison.Ordinal);
        await _authentication.DidNotReceiveWithAnyArgs().SignInAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Logout_signs_out_and_goes_home()
    {
        var result = await Create(Voter()).Logout();

        AssertRedirect(result, "Index", "Home");
        await _authentication.Received(1).SignOutAsync(Arg.Any<HttpContext>(), "Cookies", Arg.Any<AuthenticationProperties?>());
    }

    [Fact]
    public async Task ChangePassword_post_success_sets_status_and_redirects()
    {
        _users.ChangePasswordAsync(5, Arg.Any<ChangePassword>()).Returns(true);
        var controller = Create(Voter(5));

        var result = await controller.ChangePassword(new ChangePassword { Password = "a", NewPassword = "Secret#Pass1", ConfirmNewPassword = "Secret#Pass1" });

        AssertRedirect(result, "ChangePassword");
        Assert.Equal("Hasło zostało zmienione.", controller.TempData["StatusMessage"]);
    }

    [Fact]
    public async Task ChangePassword_post_failure_flags_current_password()
    {
        _users.ChangePasswordAsync(5, Arg.Any<ChangePassword>()).Returns(false);
        var controller = Create(Voter(5));

        var result = await controller.ChangePassword(new ChangePassword { Password = "a", NewPassword = "Secret#Pass1", ConfirmNewPassword = "Secret#Pass1" });

        AssertView(result);
        Assert.NotEmpty(controller.ModelState["Password"]!.Errors);
    }

    [Fact]
    public async Task Activation_returns_service_result_and_rejects_missing_id()
    {
        var code = Guid.NewGuid();
        _users.ActivateAsync(code).Returns(true);

        Assert.Equal(true, AssertView(await Create().Activation(code)).Model);
        Assert.Equal(false, AssertView(await Create().Activation(null)).Model);
        await _users.Received(1).ActivateAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task PasswordRecovery_post_always_shows_confirmation()
    {
        var result = await Create().PasswordRecovery(new PasswordRecovery { Email = "nobody@example.com" });

        AssertView(result, "PasswordRecoveryConfirmation");
        await _users.Received(1).RequestPasswordResetAsync("nobody@example.com", Arg.Any<Func<Guid, string>>());
    }

    [Fact]
    public async Task ResetPassword_get_validates_token()
    {
        var token = Guid.NewGuid();
        _users.IsPasswordResetTokenValidAsync(token).Returns(true);

        var ok = AssertView(await Create().ResetPassword(token));
        Assert.Equal(token, Assert.IsType<ResetPasswordViewModel>(ok.Model).Token);
        AssertView(await Create().ResetPassword(Guid.NewGuid()), "ResetPasswordInvalid");
        AssertView(await Create().ResetPassword((Guid?)null), "ResetPasswordInvalid");
    }

    [Fact]
    public async Task ResetPassword_post_redirects_to_login_on_success()
    {
        var token = Guid.NewGuid();
        _users.ResetPasswordAsync(token, "Secret#Pass1").Returns(true);
        var controller = Create();

        var result = await controller.ResetPassword(new ResetPasswordViewModel { Token = token, NewPassword = "Secret#Pass1", ConfirmNewPassword = "Secret#Pass1" });

        AssertRedirect(result, "Login");
        Assert.NotNull(controller.TempData["StatusMessage"]);
    }

    [Fact]
    public async Task Search_skips_service_for_blank_input()
    {
        var blank = AssertView(await Create().Search("  "));
        Assert.False(Assert.IsType<VoteSearchViewModel>(blank.Model).Searched);
        await _results.DidNotReceiveWithAnyArgs().SearchVoteAsync(default!);

        _results.SearchVoteAsync("abc").Returns(new VoteSearchViewModel { Searched = true, Hash = "ABC" });
        var searched = AssertView(await Create().Search("abc"));
        Assert.True(Assert.IsType<VoteSearchViewModel>(searched.Model).Searched);
    }
}

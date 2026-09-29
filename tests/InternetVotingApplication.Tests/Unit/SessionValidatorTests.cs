using System.Security.Claims;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InternetVotingApplication.Tests.Unit;

public class SessionValidatorTests
{
    private readonly IUserService _users = Substitute.For<IUserService>();
    private readonly IAuthenticationService _authentication = Substitute.For<IAuthenticationService>();

    private CookieValidatePrincipalContext Context(ClaimsPrincipal principal)
    {
        var services = new ServiceCollection().AddSingleton(_users).AddSingleton(_authentication).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);
        return new CookieValidatePrincipalContext(http, scheme, new CookieAuthenticationOptions(), ticket);
    }

    private static ClaimsPrincipal Session(string? userId = "7", string role = Roles.Voter, string? stamp = "STAMP")
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role), new(ClaimTypes.Name, "Jan Kowalski") };
        if (userId != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (stamp != null)
        {
            claims.Add(new Claim(SessionValidator.PasswordStampClaim, stamp));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    [Fact]
    public async Task Unchanged_account_keeps_the_session_as_it_is()
    {
        _users.GetSessionStateAsync(7).Returns(new SessionState(true, false, "STAMP"));
        var context = Context(Session());
        var before = context.Principal;

        await SessionValidator.ValidateAsync(context);

        Assert.Same(before, context.Principal);
        Assert.False(context.ShouldRenew);
        await _authentication.DidNotReceiveWithAnyArgs().SignOutAsync(default!, default, default);
    }

    public static TheoryData<string?, SessionState?, string?> Rejected => new()
    {
        { null, null, "STAMP" },                                   // no user id in the cookie
        { "7", null, "STAMP" },                                    // account deleted
        { "7", new SessionState(false, false, "STAMP"), "STAMP" }, // account deactivated
        { "7", new SessionState(true, false, "NEW"), "STAMP" },    // password changed elsewhere
        { "7", new SessionState(true, false, "STAMP"), null },     // cookie from before password stamps
    };

    [Theory]
    [MemberData(nameof(Rejected))]
    public async Task Session_is_ended_when_the_account_no_longer_matches(string? userId, SessionState? state, string? stamp)
    {
        _users.GetSessionStateAsync(7).Returns(state);
        var context = Context(Session(userId, stamp: stamp));

        await SessionValidator.ValidateAsync(context);

        Assert.Null(context.Principal);
        await _authentication.Received(1).SignOutAsync(Arg.Any<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, Arg.Any<AuthenticationProperties?>());
    }

    [Theory]
    [InlineData(Roles.Voter, true, Roles.Admin)]
    [InlineData(Roles.Admin, false, Roles.Voter)]
    public async Task Changed_role_is_written_into_the_session_at_once(string cookieRole, bool isAdmin, string expectedRole)
    {
        _users.GetSessionStateAsync(7).Returns(new SessionState(true, isAdmin, "STAMP"));
        var context = Context(Session(role: cookieRole));

        await SessionValidator.ValidateAsync(context);

        Assert.True(context.Principal!.IsInRole(expectedRole));
        Assert.False(context.Principal.IsInRole(cookieRole));
        Assert.Equal("Jan Kowalski", context.Principal.Identity!.Name);
        Assert.True(context.ShouldRenew);
    }
}

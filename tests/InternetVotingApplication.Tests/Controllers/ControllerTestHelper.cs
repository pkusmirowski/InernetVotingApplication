using System.Security.Claims;
using InternetVotingApplication.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InternetVotingApplication.Tests.Controllers;

internal static class ControllerTestHelper
{
    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    public static ClaimsPrincipal Voter(int id = 1, bool admin = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(ClaimTypes.Email, "user@example.com"),
            new(ClaimTypes.Name, "Jan Kowalski"),
            new(ClaimTypes.Role, admin ? Roles.Admin : Roles.Voter),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>Gives a controller a usable HttpContext, TempData and Url helper without hosting a server.</summary>
    public static T Prepare<T>(this T controller, ClaimsPrincipal? user = null, IAuthenticationService? authentication = null)
        where T : Controller
    {
        var services = new ServiceCollection();
        services.AddSingleton(authentication ?? Substitute.For<IAuthenticationService>());
        var httpContext = new DefaultHttpContext
        {
            User = user ?? Anonymous(),
            RequestServices = services.BuildServiceProvider(),
        };
        httpContext.Request.Scheme = "https";

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());

        var url = Substitute.For<IUrlHelper>();
        // Like the real helper: absolute when a protocol is requested, otherwise a site-relative path.
        url.Action(Arg.Any<UrlActionContext>()).Returns(ci =>
        {
            var context = ci.Arg<UrlActionContext>();
            var path = $"/{context.Controller}/{context.Action}";
            return context.Protocol is null ? path : "https://app" + path;
        });
        url.IsLocalUrl(Arg.Any<string?>()).Returns(ci => ci.Arg<string?>() is { } s && s.StartsWith('/') && !s.StartsWith("//", StringComparison.Ordinal));
        controller.Url = url;
        return controller;
    }

    public static RedirectToActionResult AssertRedirect(IActionResult result, string action, string? controllerName = null)
    {
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(action, redirect.ActionName);
        Assert.Equal(controllerName, redirect.ControllerName);
        return redirect;
    }

    public static ViewResult AssertView(IActionResult result, string? viewName = null)
    {
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewName, view.ViewName);
        return view;
    }
}

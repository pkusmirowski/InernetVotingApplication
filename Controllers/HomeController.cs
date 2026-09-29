using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InternetVotingApplication.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }

    /// <summary>
    /// Error page for status codes. It is re-executed with the method of the failed request, so a rejected POST
    /// (for example one stopped by the rate limiter) arrives here as a POST without a valid antiforgery token;
    /// the page changes nothing, so the token is not required.
    /// </summary>
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        if (code is < 400 or > 599)
        {
            code = StatusCodes.Status404NotFound;
        }

        Response.StatusCode = code;
        return View(code);
    }
}

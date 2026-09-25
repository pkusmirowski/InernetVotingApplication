using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InternetVotingApplication.Controllers
{
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult HttpStatus(int code)
        {
            Response.StatusCode = code;
            return View(code);
        }
    }
}

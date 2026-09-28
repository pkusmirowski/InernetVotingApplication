using InternetVotingApplication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetVotingApplication.Controllers;

/// <summary>Development-only diagnostics page: which database is in use, where the mails and the key are, what to do next.</summary>
[AllowAnonymous]
[Route("setup")]
public sealed class SetupController(IHostEnvironment environment, ISetupDiagnostics diagnostics) : Controller
{
    [HttpGet("")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return View(await diagnostics.CollectAsync(cancellationToken));
    }
}

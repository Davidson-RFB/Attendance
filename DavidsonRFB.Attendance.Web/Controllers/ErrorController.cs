using Microsoft.AspNetCore.Mvc;

namespace DavidsonRFB.Attendance.Web.Controllers;

/// <summary>
/// Ported from the legacy ErrorController, with one deliberate change:
/// [Authorize] is NOT carried forward on this controller. In the legacy app,
/// hitting this (itself AAD-gated) page while anonymous was actually how an
/// anonymous Home/Index visit eventually got redirected to AAD sign-in (a
/// two-hop 403 -> gated error page -> 401 -> challenge dance). The new
/// Home/Index auth (see HomeController, Auth/KioskAuthentication.cs)
/// redirects straight to AAD itself, so this page no longer needs to be
/// authenticated to view - it's wired generically via
/// UseStatusCodePagesWithReExecute in Program.cs for any 4xx/5xx status
/// code, and gating it behind auth would just break that for anonymous
/// visitors hitting an actual error.
///
/// Deliberately NOT [HttpGet]-restricted: UseStatusCodePagesWithReExecute
/// re-executes the pipeline at these routes while preserving the ORIGINAL
/// request's HTTP method - so a failing POST (e.g. a rejected anti-forgery
/// token, a 400/403/404/etc. from any mutating action) re-executes here as a
/// POST too. An [HttpGet]-only constraint made that re-executed POST itself
/// fail routing with a genuine 405, which silently overwrote and masked the
/// real original status code by the time it reached the client - caught via
/// live integration testing (a rejected anti-forgery POST was showing up as
/// 405 instead of 400) - see docs/DOTNET10_MIGRATION_PLAN.md Decisions log.
/// These actions must accept any verb, since they're never navigated to
/// directly by a user with a specific verb in mind.
/// </summary>
public class ErrorController : Controller
{
    // Both of these deliberately shadow ControllerBase helpers of the same
    // name (Unauthorized()/StatusCode(int)) - they're the action methods for
    // the /Error/... routes below, not the result-factory helpers.
    [Route("Error/Unauthorized")]
    public new IActionResult Unauthorized()
    {
        return View();
    }

    [Route("Error/StatusCode/{code:int}")]
    public new IActionResult StatusCode(int code)
    {
        if (code == StatusCodes.Status403Forbidden)
        {
            return View("Unauthorized");
        }

        ViewBag.StatusCode = code;
        return View();
    }
}

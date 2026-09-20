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
/// </summary>
public class ErrorController : Controller
{
    // Both of these deliberately shadow ControllerBase helpers of the same
    // name (Unauthorized()/StatusCode(int)) - they're the action methods for
    // the /Error/... routes below, not the result-factory helpers.
    [HttpGet]
    [Route("Error/Unauthorized")]
    public new IActionResult Unauthorized()
    {
        return View();
    }

    [HttpGet]
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

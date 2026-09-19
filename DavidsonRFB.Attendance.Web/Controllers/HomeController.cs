using System.Diagnostics;
using System.Data.Entity;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DavidsonRFB.Attendance.Web.Auth;
using DavidsonRFB.Attendance.Web.DAL;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.Controllers;

// NOTE: Index/Privacy/Error below are still the Phase 0 scaffolding
// placeholders as far as their *content* goes - Phase 3 replaces Index's
// body with the real clock-in/out page and removes Privacy. The
// [Authorize(AuthenticationSchemes = KioskAuthentication.HomeAndKioskSchemes)]
// on Index, and CreateCookie/RemoveCookie, are the real, final Phase 2
// implementation - see docs/DOTNET10_MIGRATION_PLAN.md section 4. Phase 3
// must carry this same attribute onto Index's real implementation and add it
// to ClockOut once that action exists.
public class HomeController : Controller
{
    private readonly AttendanceContext _db;

    public HomeController(AttendanceContext db)
    {
        _db = db;
    }

    [Authorize(AuthenticationSchemes = KioskAuthentication.HomeAndKioskSchemes)]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Mints the long-lived Kiosk cookie for the current browser, scoped to
    // the signed-in AAD user's brigade. Strictly AAD-only - see the design
    // intent in docs/DOTNET10_MIGRATION_PLAN.md section 4. This must never
    // be relaxed to allow anonymous/Kiosk-cookie access.
    [Authorize]
    public async Task<IActionResult> CreateCookie()
    {
        var email = User.FindFirst("preferred_username")?.Value ?? User.Identity?.Name;

        var employee = email is null
            ? null
            : await _db.Employees.SingleOrDefaultAsync(e => e.Email == email);

        if (employee != null)
        {
            var identity = new ClaimsIdentity(KioskAuthentication.Scheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, employee.Name));
            identity.AddClaim(new Claim(
                KioskAuthentication.BrigadeIdClaimType,
                employee.BrigadeId.ToString(CultureInfo.InvariantCulture)));

            await HttpContext.SignInAsync(
                KioskAuthentication.Scheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddYears(10),
                });
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> RemoveCookie()
    {
        await HttpContext.SignOutAsync(KioskAuthentication.Scheme);
        return RedirectToAction(nameof(Index));
    }
}

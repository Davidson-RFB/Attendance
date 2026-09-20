using System.Diagnostics;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DavidsonRFB.Attendance.Web.Auth;
using DavidsonRFB.Attendance.Web.DAL;
using DavidsonRFB.Attendance.Web.Helpers;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.Controllers;

/// <summary>
/// The kiosk sign-in/out page. Index and ClockOut are ported near-verbatim
/// from the legacy HomeController for behavior parity - including one
/// pre-existing quirk carried forward unchanged (see the POST Index note
/// below). CreateCookie/RemoveCookie are the Phase 2 auth implementation.
/// See docs/DOTNET10_MIGRATION_PLAN.md section 4 for the auth design intent.
/// </summary>
public class HomeController : Controller
{
    private readonly AttendanceContext _db;

    public HomeController(AttendanceContext db)
    {
        _db = db;
    }

    [Authorize(AuthenticationSchemes = KioskAuthentication.HomeAndKioskSchemes)]
    [HttpGet]
    public IActionResult Index()
    {
        var brigadeId = CurrentBrigade.Resolve(User, _db);

        var employees = _db.Employees.Where(e => e.IsActive && e.BrigadeId == brigadeId).OrderBy(e => e.Name).ToList();
        employees.Insert(0, new Employee());
        ViewBag.Employees = employees;

        var jobs = _db.Jobs.Where(j => j.BrigadeId == brigadeId).OrderBy(j => j.Description).ToList();
        jobs.Insert(0, new Job());
        ViewBag.Jobs = jobs;

        Response.Headers.Append("Refresh", "600");
        return View(new Attendees { CurrentAttendance = _db.Attendances.Where(a => !a.EndDateTime.HasValue && a.Employee.BrigadeId == brigadeId).ToList() });
    }

    [Authorize(AuthenticationSchemes = KioskAuthentication.HomeAndKioskSchemes)]
    [HttpPost]
    public IActionResult Index(Attendees attendees)
    {
        var brigadeId = CurrentBrigade.Resolve(User, _db);

        if (ModelState.IsValid)
        {
            var currentAttendance = _db.Attendances.FirstOrDefault(a => a.EmployeeId == attendees.EmployeeId &&
                                                                         !a.EndDateTime.HasValue);
            if (currentAttendance == null)
            {
                // Clock in
                _db.Attendances.Add(new Models.Attendance
                {
                    EmployeeId = attendees.EmployeeId,
                    JobId = attendees.JobId,
                    StartDateTime = DateTime.Now
                });
                ViewBag.Message = $"{_db.Employees.Single(e => e.Id == attendees.EmployeeId).Name} has been clocked in";
            }
            _db.SaveChanges();
        }

        var employees = _db.Employees.Where(e => e.IsActive && e.BrigadeId == brigadeId).OrderBy(e => e.Name).ToList();
        employees.Insert(0, new Employee());
        ViewBag.Employees = employees;

        var jobs = _db.Jobs.Where(j => j.BrigadeId == brigadeId).OrderBy(j => j.Description).ToList();
        jobs.Insert(0, new Job());
        ViewBag.Jobs = jobs;

        Response.Headers.Append("Refresh", "600");

        // NOTE: unlike the GET action above, this does NOT filter by
        // brigadeId - carried forward unchanged from the legacy app (a
        // pre-existing inconsistency, not introduced by this migration; see
        // docs/DOTNET10_MIGRATION_PLAN.md Decisions log).
        return View(new Attendees { CurrentAttendance = _db.Attendances.Where(a => !a.EndDateTime.HasValue).ToList() });
    }

    [Authorize(AuthenticationSchemes = KioskAuthentication.HomeAndKioskSchemes)]
    [HttpPost]
    public IActionResult ClockOut(Attendees attendees)
    {
        if (attendees.CurrentAttendance != null)
        {
            foreach (var attendee in attendees.CurrentAttendance.Where(a => a.Selected))
            {
                // Clock out
                var attendance = _db.Attendances.FirstOrDefault(a => a.Id == attendee.Id && !a.EndDateTime.HasValue);
                if (attendance != null)
                {
                    attendance.EndDateTime = DateTime.Now;
                }
            }
            _db.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }

    // Mints the long-lived Kiosk cookie for the current browser, scoped to
    // the signed-in AAD user's brigade. Strictly AAD-only - see the design
    // intent in docs/DOTNET10_MIGRATION_PLAN.md section 4. This must never
    // be relaxed to allow anonymous/Kiosk-cookie access.
    [Authorize(AuthenticationSchemes = KioskAuthentication.AadOnlyScheme)]
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

    [Authorize(AuthenticationSchemes = KioskAuthentication.AadOnlyScheme)]
    public async Task<IActionResult> RemoveCookie()
    {
        await HttpContext.SignOutAsync(KioskAuthentication.Scheme);
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

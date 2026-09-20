using System.Data.Entity;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DavidsonRFB.Attendance.Web.Auth;
using DavidsonRFB.Attendance.Web.DAL;
using DavidsonRFB.Attendance.Web.Helpers;

namespace DavidsonRFB.Attendance.Web.Controllers;

/// <summary>
/// Attendance record maintenance (search/create/edit/delete) - AAD-only,
/// ported near-verbatim from the legacy AttendanceController.
/// </summary>
[Authorize(AuthenticationSchemes = KioskAuthentication.AadOnlyScheme)]
public class AttendanceController : Controller
{
    private readonly AttendanceContext _db;

    public AttendanceController(AttendanceContext db)
    {
        _db = db;
    }

    // GET: Attendance
    public IActionResult Index(int? employeeId, int? jobId, DateTime? fromDate, DateTime? toDate)
    {
        var attendanceList = new List<Models.Attendance>();

        if (employeeId.GetValueOrDefault() > 0 || jobId.GetValueOrDefault() > 0 || fromDate.HasValue || toDate.HasValue)
        {
            IQueryable<Models.Attendance> attendances = _db.Attendances;
            if (employeeId.GetValueOrDefault() > 0)
            {
                attendances = attendances.Where(a => a.EmployeeId == employeeId!.Value);
            }
            if (jobId.GetValueOrDefault() > 0)
            {
                attendances = attendances.Where(a => a.JobId == jobId!.Value);
            }
            if (fromDate.HasValue)
            {
                attendances = attendances.Where(a => a.EndDateTime >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                toDate = toDate.Value.AddDays(1).AddSeconds(-1);
                attendances = attendances.Where(a => a.StartDateTime <= toDate.Value);
            }
            attendanceList = attendances.Include(a => a.Employee).Include(a => a.Job).OrderByDescending(a => a.StartDateTime).ToList();
        }

        var brigadeId = CurrentBrigade.Resolve(User, _db)!.Value;

        var employees = _db.Employees.Where(e => e.BrigadeId == brigadeId).OrderBy(e => e.Name).ToList();
        employees.Insert(0, new Models.Employee());
        ViewBag.Employees = new SelectList(employees, "Id", "Name");

        var jobs = _db.Jobs.Where(e => e.BrigadeId == brigadeId).OrderBy(j => j.Description).ToList();
        jobs.Insert(0, new Models.Job());
        ViewBag.Jobs = new SelectList(jobs, "Id", "Description");

        return View(attendanceList);
    }

    // GET: Attendance/Create
    public IActionResult Create()
    {
        var brigadeId = CurrentBrigade.Resolve(User, _db)!.Value;
        ViewBag.Employees = new SelectList(_db.Employees.Where(e => e.BrigadeId == brigadeId).OrderBy(e => e.Name), "Id", "Name");
        ViewBag.Jobs = new SelectList(_db.Jobs.Where(j => j.BrigadeId == brigadeId).OrderBy(j => j.Description), "Id", "Description");
        return View();
    }

    // POST: Attendance/Create
    // NOTE: the legacy app had [ValidateAntiForgeryToken] and
    // Html.AntiForgeryToken() both commented out here (and left inconsistent
    // with Edit/Delete, which had the attribute active but the token still
    // commented out in the view - meaning those would have failed anti-forgery
    // validation on every submit). Fixed properly here: the attribute is
    // enabled on every mutating action below, and the corresponding views
    // (Views/Attendance/{Create,Edit,Delete}.cshtml) emit the token.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("Id,EmployeeId,JobId,StartDateTime,EndDateTime")] Models.Attendance attendance)
    {
        if (ModelState.IsValid)
        {
            _db.Attendances.Add(attendance);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        var brigadeId = CurrentBrigade.Resolve(User, _db)!.Value;
        ViewBag.Employees = new SelectList(_db.Employees.Where(e => e.BrigadeId == brigadeId).OrderBy(e => e.Name), "Id", "Name");
        ViewBag.Jobs = new SelectList(_db.Jobs.Where(j => j.BrigadeId == brigadeId).OrderBy(j => j.Description), "Id", "Description");
        return View(attendance);
    }

    // GET: Attendance/Edit/5
    public IActionResult Edit(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var attendance = _db.Attendances.Find(id);
        if (attendance == null)
        {
            return NotFound();
        }

        var brigadeId = CurrentBrigade.Resolve(User, _db)!.Value;
        ViewBag.Employees = new SelectList(_db.Employees.Where(e => e.BrigadeId == brigadeId).OrderBy(e => e.Name), "Id", "Name");
        ViewBag.Jobs = new SelectList(_db.Jobs.Where(j => j.BrigadeId == brigadeId).OrderBy(j => j.Description), "Id", "Description");
        return View(attendance);
    }

    // POST: Attendance/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit([Bind("Id,EmployeeId,JobId,StartDateTime,EndDateTime")] Models.Attendance attendance)
    {
        if (ModelState.IsValid)
        {
            _db.Entry(attendance).State = EntityState.Modified;
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        var brigadeId = CurrentBrigade.Resolve(User, _db)!.Value;
        ViewBag.Employees = new SelectList(_db.Employees.Where(e => e.BrigadeId == brigadeId).OrderBy(e => e.Name), "Id", "Name");
        ViewBag.Jobs = new SelectList(_db.Jobs.Where(j => j.BrigadeId == brigadeId).OrderBy(j => j.Description), "Id", "Description");
        return View(attendance);
    }

    // GET: Attendance/Delete/5
    public IActionResult Delete(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var attendance = _db.Attendances.Find(id);
        if (attendance == null)
        {
            return NotFound();
        }

        return View(attendance);
    }

    // POST: Attendance/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var attendance = _db.Attendances.Find(id);
        if (attendance != null)
        {
            _db.Attendances.Remove(attendance);
            _db.SaveChanges();
        }
        return RedirectToAction(nameof(Index));
    }
}

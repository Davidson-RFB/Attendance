using System.Data.Entity;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DavidsonRFB.Attendance.Web.Auth;
using DavidsonRFB.Attendance.Web.DAL;
using DavidsonRFB.Attendance.Web.Helpers;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.Controllers;

/// <summary>
/// Employee maintenance (list/create/edit) - AAD-only, ported from the
/// legacy EmployeeController. Delete/DeleteConfirmed were NOT ported: they
/// had no link anywhere in Views/Employee/Index.cshtml (unreachable via the
/// UI) and the GET action's view (Views/Employee/Delete.cshtml) didn't even
/// exist in the legacy app - it would have thrown at runtime if ever hit
/// directly by URL. Dropped as dead/broken code, not a behavior change for
/// any real user.
/// </summary>
[Authorize(AuthenticationSchemes = KioskAuthentication.AadOnlyScheme)]
public class EmployeeController : Controller
{
    private readonly AttendanceContext _db;

    public EmployeeController(AttendanceContext db)
    {
        _db = db;
    }

    // GET: Employee
    public IActionResult Index()
    {
        var brigadeId = CurrentBrigade.Resolve(User, _db);

        var employees = _db.Employees.Where(e => e.BrigadeId == brigadeId).Include(e => e.Rank).OrderBy(e => e.Name);
        return View(employees.ToList());
    }

    // GET: Employee/Create
    public IActionResult Create()
    {
        ViewBag.Ranks = new SelectList(_db.Ranks.OrderBy(r => r.Description), "Id", "Description");
        return View();
    }

    // POST: Employee/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("Id,Name,RankId,Email,IsActive")] Employee employee)
    {
        if (ModelState.IsValid)
        {
            employee.BrigadeId = CurrentBrigade.Resolve(User, _db)!.Value;
            _db.Employees.Add(employee);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Ranks = new SelectList(_db.Ranks.OrderBy(r => r.Description), "Id", "Description", employee.RankId);
        return View(employee);
    }

    // GET: Employee/Edit/5
    public IActionResult Edit(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var brigadeId = CurrentBrigade.Resolve(User, _db);
        var employee = _db.Employees.SingleOrDefault(e => e.Id == id && e.BrigadeId == brigadeId);

        if (employee == null)
        {
            return NotFound();
        }

        ViewBag.Ranks = new SelectList(_db.Ranks.OrderBy(r => r.Description), "Id", "Description", employee.RankId);
        return View(employee);
    }

    // POST: Employee/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit([Bind("Id,Name,RankId,Email,IsActive")] Employee employee)
    {
        if (ModelState.IsValid)
        {
            employee.BrigadeId = CurrentBrigade.Resolve(User, _db)!.Value;
            _db.Entry(employee).State = EntityState.Modified;
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Ranks = new SelectList(_db.Ranks.OrderBy(r => r.Description), "Id", "Description", employee.RankId);
        return View(employee);
    }
}

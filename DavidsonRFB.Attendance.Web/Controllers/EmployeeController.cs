using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using DavidsonRFB.Attendance.Web.DAL;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.Controllers
{
    //[Authorize]
    [Authorize]
    //[Authorize]
    public class EmployeeController : Controller
    {
        private AttendanceContext context = new AttendanceContext();

        // GET: Employee
        public ActionResult Index()
        {
            int? brigadeId = Helpers.Brigade.CurrentBrigade();

            var employees = context.Employees.Where(e => e.BrigadeId == brigadeId).Include(e => e.Rank);
            return View(employees.ToList());
        }

        // GET: Employee/Create
        public ActionResult Create()
        {
            ViewBag.Ranks = new SelectList(context.Ranks, "Id", "Description");
            return View();
        }

        // POST: Employee/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,RankId,Email,IsActive")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.BrigadeId = Helpers.Brigade.CurrentBrigade().Value;
                context.Employees.Add(employee);
                context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Ranks = new SelectList(context.Ranks, "Id", "Description", employee.RankId);
            return View(employee);
        }

        // GET: Employee/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int? brigadeId = Helpers.Brigade.CurrentBrigade();
            Employee employee = context.Employees.Single(e => e.Id == id && e.BrigadeId == brigadeId);

            if (employee == null)
            {
                return HttpNotFound();
            }

            ViewBag.Ranks = new SelectList(context.Ranks, "Id", "Description", employee.RankId);
            return View(employee);
        }

        // POST: Employee/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,RankId,Email,IsActive")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.BrigadeId = Helpers.Brigade.CurrentBrigade().Value;
                context.Entry(employee).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Ranks = new SelectList(context.Ranks, "Id", "Description", employee.RankId);
            return View(employee);
        }

        // GET: Employee/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int? brigadeId = Helpers.Brigade.CurrentBrigade();
            Employee employee = context.Employees.Single(e => e.Id == id && e.BrigadeId == brigadeId);

            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            int? brigadeId = Helpers.Brigade.CurrentBrigade();
            Employee employee = context.Employees.Single(e => e.Id == id && e.BrigadeId == brigadeId);
            context.Employees.Remove(employee);
            context.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

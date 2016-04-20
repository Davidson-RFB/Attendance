using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using DavidsonRFB.Attendance.Web.DAL;

namespace DavidsonRFB.Attendance.Web.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private AttendanceContext db = new AttendanceContext();

        // GET: Attendance
        public ActionResult Index()
        {
            var attendances = db.Attendances.Include(a => a.Employee).Include(a => a.Job);
            return View(attendances.ToList());
        }

        // GET: Attendance/Create
        public ActionResult Create()
        {
            int brigadeId = Helpers.Brigade.CurrentBrigade().Value;
            ViewBag.Employees = new SelectList(db.Employees.Where(e => e.BrigadeId == brigadeId), "Id", "Name");
            ViewBag.Jobs = new SelectList(db.Jobs.Where(j => j.BrigadeId == brigadeId), "Id", "Description");
            return View();
        }

        // POST: Attendance/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,EmployeeId,JobId,StartDateTime,EndDateTime")] Models.Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                db.Attendances.Add(attendance);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            int brigadeId = Helpers.Brigade.CurrentBrigade().Value;
            ViewBag.Employees = new SelectList(db.Employees.Where(e => e.BrigadeId == brigadeId), "Id", "Name");
            ViewBag.Jobs = new SelectList(db.Jobs.Where(j => j.BrigadeId == brigadeId), "Id", "Description");
            return View(attendance);
        }

        // GET: Attendance/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Models.Attendance attendance = db.Attendances.Find(id);
            if (attendance == null)
            {
                return HttpNotFound();
            }

            int brigadeId = Helpers.Brigade.CurrentBrigade().Value;
            ViewBag.Employees = new SelectList(db.Employees.Where(e => e.BrigadeId == brigadeId), "Id", "Name");
            ViewBag.Jobs = new SelectList(db.Jobs.Where(j => j.BrigadeId == brigadeId), "Id", "Description");
            return View(attendance);
        }

        // POST: Attendance/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,EmployeeId,JobId,StartDateTime,EndDateTime")] Models.Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                db.Entry(attendance).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            int brigadeId = Helpers.Brigade.CurrentBrigade().Value;
            ViewBag.Employees = new SelectList(db.Employees.Where(e => e.BrigadeId == brigadeId), "Id", "Name");
            ViewBag.Jobs = new SelectList(db.Jobs.Where(j => j.BrigadeId == brigadeId), "Id", "Description");
            return View(attendance);
        }

        // GET: Attendance/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Models.Attendance attendance = db.Attendances.Find(id);
            if (attendance == null)
            {
                return HttpNotFound();
            }

            return View(attendance);
        }

        // POST: Attendance/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Models.Attendance attendance = db.Attendances.Find(id);
            db.Attendances.Remove(attendance);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

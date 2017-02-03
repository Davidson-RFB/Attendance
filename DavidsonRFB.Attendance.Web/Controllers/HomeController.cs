using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DavidsonRFB.Attendance.Web.DAL;
using DavidsonRFB.Attendance.Web.Filters;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.Controllers
{
    public class HomeController : Controller
    {
        private AttendanceContext context = new AttendanceContext();

        [LocalAuthorization]
        [HttpGet]
        public ActionResult Index()
        {
            int? brigadeId = Helpers.Brigade.CurrentBrigade();

            var employees = context.Employees.Where(e => e.IsActive && e.BrigadeId == brigadeId).OrderBy(e => e.Name).ToList();
            employees.Insert(0, new Employee());
            ViewBag.Employees = employees;

            var jobs = context.Jobs.Where(j => j.BrigadeId == brigadeId).OrderBy(j => j.Description).ToList();
            jobs.Insert(0, new Job());
            ViewBag.Jobs = jobs;

            Response.AddHeader("Refresh", "600");
            return View(new Attendees() { CurrentAttendance = context.Attendances.Where(a => !a.EndDateTime.HasValue && a.Employee.BrigadeId == brigadeId).ToList() });
        }

        [LocalAuthorization]
        [HttpPost]
        public ActionResult Index(Attendees attendees)
        {
            int? brigadeId = Helpers.Brigade.CurrentBrigade();

            if (ModelState.IsValid)
            {
                var currentAttendance = context.Attendances.FirstOrDefault(a => a.EmployeeId == attendees.EmployeeId && 
                                                                               !a.EndDateTime.HasValue);
                if (currentAttendance == null)
                {
                    // Clock in
                    context.Attendances.Add(new Models.Attendance()
                    {
                        EmployeeId = attendees.EmployeeId,
                        JobId = attendees.JobId,
                        StartDateTime = DateTime.Now
                    });
                    ViewBag.Message = string.Format("{0} has been clocked in", context.Employees.Single(e => e.Id == attendees.EmployeeId).Name);
                }
                context.SaveChanges();
            }

            var employees = context.Employees.Where(e => e.IsActive && e.BrigadeId == brigadeId).OrderBy(e => e.Name).ToList();
            employees.Insert(0, new Employee());
            ViewBag.Employees = employees;

            var jobs = context.Jobs.Where(j => j.BrigadeId == brigadeId).OrderBy(j => j.Description).ToList();
            jobs.Insert(0, new Job());
            ViewBag.Jobs = jobs;

            Response.AddHeader("Refresh", "600");
            return View(new Attendees() { CurrentAttendance = context.Attendances.Where(a => !a.EndDateTime.HasValue).ToList() });
        }

        [LocalAuthorization]
        [HttpPost]
        public ActionResult ClockOut(Attendees attendees)
        {
            if (attendees.CurrentAttendance != null)
            {
                foreach (var attendee in attendees.CurrentAttendance.Where(a => a.Selected))
                {
                    // Clock out
                    var attendance = context.Attendances.FirstOrDefault(a => a.Id == attendee.Id && !a.EndDateTime.HasValue);
                    attendance.EndDateTime = DateTime.Now;
                }
                context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult CreateCookie()
        {
            Employee employee = context.Employees.Single(e => e.Email == User.Identity.Name);
            if (employee != null)
            {
                var authTicket = new FormsAuthenticationTicket(
                    1,                             // version
                    employee.BrigadeId.ToString(), // name
                    DateTime.Now,                  // created
                    DateTime.Now.AddYears(10),     // expires
                    true,                          // persistent?
                    "Log Time"                     // can be used to store roles
                    );

                string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                authCookie.Expires = authTicket.Expiration;

                ControllerContext.HttpContext.Response.Cookies.Add(authCookie);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public ActionResult RemoveCookie()
        {
            HttpCookie authCookie = ControllerContext.HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName];
            authCookie.Expires = DateTime.Now;
            ControllerContext.HttpContext.Response.Cookies.Add(authCookie);
            return RedirectToAction("Index", "Home");
        }
    }
}
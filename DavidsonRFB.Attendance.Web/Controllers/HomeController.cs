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
    //[Authorize(Roles = "Log Time")]
    public class HomeController : Controller
    {
        [LocalAuthorization]
        [HttpGet]
        public ActionResult Index()
        {
            AttendanceContext context = new AttendanceContext();

            var employees = context.Employees.Where(e => e.IsActive).OrderBy(e => e.Name).ToList();
            employees.Insert(0, new Employee());
            ViewBag.Employees = employees;

            var jobs = context.Jobs.OrderBy(j => j.Description).ToList();
            jobs.Insert(0, new Job());
            ViewBag.Jobs = jobs;

            return View(new Attendees() { CurrentAttendance = context.Attendances.Where(a => !a.EndDateTime.HasValue).ToList() });
        }

        [LocalAuthorization]
        [HttpPost]
        public ActionResult Index(Attendees attendees)
        {
            AttendanceContext context = new AttendanceContext();

            if (attendees.EmployeeId > 0)
            {
                var currentAttendance = context.Attendances.FirstOrDefault(a => a.EmployeeId == attendees.EmployeeId && !a.EndDateTime.HasValue);
                if (currentAttendance == null)
                {
                    // Clock in
                    if (attendees.JobId > 0)
                    {
                        context.Attendances.Add(new Models.Attendance()
                        {
                            EmployeeId = attendees.EmployeeId,
                            JobId = attendees.JobId,
                            StartDateTime = DateTime.Now
                        });
                        ViewBag.Message = string.Format("{0} has been clocked in", context.Employees.Single(e => e.Id == attendees.EmployeeId).Name);
                    }
                    else
                    {
                        ViewBag.Message = "Job description is required";
                    }
                }
                else
                {
                    // Clock out
                    currentAttendance.EndDateTime = DateTime.Now;
                    ViewBag.Message = string.Format("{0} has been clocked out", currentAttendance.Employee.Name);
                }
                context.SaveChanges();
            }

            var employees = context.Employees.Where(e => e.IsActive).OrderBy(e => e.Name).ToList();
            employees.Insert(0, new Employee());
            ViewBag.Employees = employees;

            var jobs = context.Jobs.OrderBy(j => j.Description).ToList();
            jobs.Insert(0, new Job());
            ViewBag.Jobs = jobs;

            return View(new Attendees() { CurrentAttendance = context.Attendances.Where(a => !a.EndDateTime.HasValue).ToList() });
        }

        [LocalAuthorization]
        [HttpGet]
        public ActionResult ClockOutAll()
        {
            AttendanceContext context = new AttendanceContext();

            foreach (var attendance in context.Attendances.Where(a => !a.EndDateTime.HasValue))
            {
                // Clock out
                attendance.EndDateTime = DateTime.Now;
            }
            context.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult CreateCookie()
        {
            var authTicket = new FormsAuthenticationTicket(
                1,                             // version
                "AttendancePC",                // user name
                DateTime.Now,                  // created
                DateTime.Now.AddYears(10),     // expires
                true,                          // persistent?
                "Log Time"                     // can be used to store roles
                );

            string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
            var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            authCookie.Expires = authTicket.Expiration;

            ControllerContext.HttpContext.Response.Cookies.Add(authCookie);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult RemoveCookie()
        {
            HttpCookie authCookie = ControllerContext.HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName];
            authCookie.Expires = DateTime.Now;
            ControllerContext.HttpContext.Response.Cookies.Add(authCookie);
            return RedirectToAction("Index", "Home");
        }
    }
}
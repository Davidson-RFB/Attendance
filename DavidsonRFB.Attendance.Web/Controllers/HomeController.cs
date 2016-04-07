using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.Controllers
{
    public class HomeController : Controller
    {
        private List<RankModel> Ranks
        {
            get
            {
                List<RankModel> ranks = new List<RankModel>();
                ranks.Add(new RankModel() { RankId = 1, Description = "Captain", ImageSource = "", IsActive = true });
                ranks.Add(new RankModel() { RankId = 2, Description = "Senior Deputy Captain", ImageSource = "", IsActive = true });
                ranks.Add(new RankModel() { RankId = 3, Description = "Deputy Captain", ImageSource = "", IsActive = true });
                ranks.Add(new RankModel() { RankId = 4, Description = "Firefighter", ImageSource = "", IsActive = true });
                ranks.Add(new RankModel() { RankId = 5, Description = "Trainee", ImageSource = "", IsActive = true });
                ranks.Add(new RankModel() { RankId = 6, Description = "Group Officer", ImageSource = "", IsActive = true });
                return ranks;
            }
        }

        private List<JobModel> Jobs
        {
            get
            {
                List<JobModel> Jobs = new List<JobModel>();
                Jobs.Add(new JobModel() { JobId = 1, Description = "Duty Crew", IsActive = true });
                Jobs.Add(new JobModel() { JobId = 1, Description = "Fire Call", IsActive = true });
                Jobs.Add(new JobModel() { JobId = 1, Description = "Hazard Reduction", IsActive = true });
                Jobs.Add(new JobModel() { JobId = 1, Description = "Training", IsActive = true });
                Jobs.Add(new JobModel() { JobId = 1, Description = "Community Engagement", IsActive = true });
                return Jobs;
            }
        }

        private List<EmployeeModel> Employees
        {
            get
            {
                List<EmployeeModel> employees = new List<EmployeeModel>();
                employees.Add(new EmployeeModel() { EmployeeId = 1, Name = "Jason King", EmailAddress = "jason@blah.com", RankId = 3, IsActive = true, Rank = Ranks.Single(r => r.RankId == 3) });
                employees.Add(new EmployeeModel() { EmployeeId = 2, Name = "Rolf Krolke", EmailAddress = "rolf@blah.com", RankId = 6, IsActive = true, Rank = Ranks.Single(r => r.RankId == 6) });
                employees.Add(new EmployeeModel() { EmployeeId = 3, Name = "Blake Dutton", EmailAddress = "blake@blah.com", RankId = 4, IsActive = true, Rank = Ranks.Single(r => r.RankId == 4) });
                employees.Add(new EmployeeModel() { EmployeeId = 4, Name = "James McCutcheon", EmailAddress = "james@blah.com", RankId = 4, IsActive = true, Rank = Ranks.Single(r => r.RankId == 4) });
                employees.Add(new EmployeeModel() { EmployeeId = 5, Name = "Tim Eliot", EmailAddress = "tim@blah.com", RankId = 3, IsActive = true, Rank = Ranks.Single(r => r.RankId == 3) });
                return employees;
            }
        }

        private List<AttendanceModel> AttendingEmployees
        {
            get
            {
                if (Session["AttendingEmployees"] == null)
                {
                    Session["AttendingEmployees"] = new List<AttendanceModel>();
                }
                return (List<AttendanceModel>)Session["AttendingEmployees"];
            }
            set
            {
                Session["AttendingEmployees"] = value;
            }
        }

        public ActionResult Index()
        {
            ViewBag.Jobs = Jobs;
            return View(AttendingEmployees);
        }
    }
}
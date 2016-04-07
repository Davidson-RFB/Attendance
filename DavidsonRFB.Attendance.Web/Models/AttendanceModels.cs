using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DavidsonRFB.Attendance.Web.Models
{
    public class JobModel
    {
        public int JobId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class RankModel
    {
        public int RankId { get; set; }
        public string Description { get; set; }
        public string ImageSource { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmployeeModel
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; }
        public string EmailAddress { get; set; }
        public int RankId { get; set; }
        public bool IsActive { get; set; }

        public RankModel Rank { get; set; }
    }

    public class AttendanceModel
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public int JobId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }

        public EmployeeModel Employee { get; set; }
        public JobModel Job { get; set; }
    }
}
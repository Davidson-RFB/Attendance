using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DavidsonRFB.Attendance.Web.Models
{
    public class Brigade
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string District { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<Employee> Attendances { get; set; }
    }

    public class Job
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class Rank
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string ImageSource { get; set; }
        public bool IsActive { get; set; }
    }

    public class Employee
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string Name { get; set; }
        public int? BrigadeId { get; set; }
        public virtual Brigade Brigade { get; set; }
        public int? RankId { get; set; }
        public virtual Rank Rank { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
    }

    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }
        public int? JobId { get; set; }
        public virtual Job Job { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }

        [NotMapped()]
        public bool Selected { get; set; }
    }

    public class Attendees
    {
        public List<Attendance> CurrentAttendance { get; set; }
        public int EmployeeId { get; set; }
        public int JobId { get; set; }
    }
}
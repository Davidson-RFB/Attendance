﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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
        public int BrigadeId { get; set; }
        public virtual Brigade Brigade { get; set; }
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
        [DisplayName("Firezone Number")]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [DisplayName("Brigade")]
        [Required]
        public int BrigadeId { get; set; }

        public virtual Brigade Brigade { get; set; }

        [DisplayName("Rank")]
        public int? RankId { get; set; }

        public virtual Rank Rank { get; set; }

        [DisplayName("Active")]
        public bool IsActive { get; set; }

        [DataType(DataType.EmailAddress)]
        [Required]
        public string Email { get; set; }

        [NotMapped]
        public double WeekHrs
        {
            get
            {
                return Attendances?.Where(a => a.StartDateTime > DateTime.Now.AddDays(-7)).Sum(a => a.TotalHours) ?? 0;
            }
        }

        public virtual ICollection<Attendance> Attendances { get; set; }
    }

    public class Attendance
    {
        public int Id { get; set; }

        [DisplayName("Member")]
        public int EmployeeId { get; set; }

        public virtual Employee Employee { get; set; }

        [DisplayName("Job")]
        public int? JobId { get; set; }

        public virtual Job Job { get; set; }

        [DisplayName("Start Time")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime StartDateTime { get; set; }

        [DisplayName("Finish Time")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(ApplyFormatInEditMode = true, ConvertEmptyStringToNull = true, DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? EndDateTime { get; set; }

        [NotMapped()]
        public bool Selected { get; set; }

        [DisplayName("Hours Attended")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        [NotMapped()]
        public double TotalHours
        {
            get { return EndDateTime.HasValue ? EndDateTime.Value.Subtract(StartDateTime).TotalHours : DateTime.Now.Subtract(StartDateTime).TotalHours; }
        }
    }

    public class Attendees
    {
        public List<Attendance> CurrentAttendance { get; set; }

        [DisplayName("Member")]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "{0} is required")]
        public int EmployeeId { get; set; }

        [DisplayName("Job")]
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "{0} is required")]
        public int JobId { get; set; }
    }
}
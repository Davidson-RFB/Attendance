using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.DAL
{
    public class AttendanceInitializer : DropCreateDatabaseIfModelChanges<AttendanceContext>
    {
        protected override void Seed(AttendanceContext context)
        {
            var brigades = new List<Brigade>()
            {
                new Brigade() { District = "Warringah/Pittwater", Name = "Davidson RFB", IsActive = true }
            };
            brigades.ForEach(b => context.Brigades.Add(b));
            context.SaveChanges();

            var rank = new List<Rank>()
            {
                new Rank() { Description = "Administration", ImageSource = null, IsActive = true },
                new Rank() { Description = "Cadet", ImageSource = null, IsActive = true },
                new Rank() { Description = "Captain", ImageSource = null, IsActive = true },
                new Rank() { Description = "Deputy Captain", ImageSource = null, IsActive = true },
                new Rank() { Description = "Deputy Group Captain", ImageSource = null, IsActive = true },
                new Rank() { Description = "Firefighter", ImageSource = null, IsActive = true },
                new Rank() { Description = "Group Captain", ImageSource = null, IsActive = true },
                new Rank() { Description = "Senior Deputy Captain", ImageSource = null, IsActive = true },
                new Rank() { Description = "Trainee", ImageSource = null, IsActive = true }
            };
            rank.ForEach(r => context.Ranks.Add(r));
            context.SaveChanges();

            var jobs = new List<Job>()
            {
                new Job() { Description = "Community Engagement", IsActive = true },
                new Job() { Description = "Duty Crew", IsActive = true },
                new Job() { Description = "Fire Call", IsActive = true },
                new Job() { Description = "Flyer", IsActive = true },
                new Job() { Description = "Hazard Reduction", IsActive = true },
                new Job() { Description = "Meeting", IsActive = true },
                new Job() { Description = "Training", IsActive = true }
            };
            jobs.ForEach(j => context.Jobs.Add(j));
            context.SaveChanges();

            var employees = new List<Employee>
            {
                new Employee() { Id = 93247, Name = "Jason King", Brigade = context.Brigades.Single(b => b.Name == "Davidson RFB"), Rank = context.Ranks.Single(r => r.Description == "Deputy Captain"), IsActive = true },
                new Employee() { Id = 1234, Name = "Rolf Krolke", Brigade = context.Brigades.Single(b => b.Name == "Davidson RFB"), Rank = context.Ranks.Single(r => r.Description == "Deputy Group Captain"), IsActive = true },
                new Employee() { Id = 105544, Name = "Blake Dutton", Brigade = context.Brigades.Single(b => b.Name == "Davidson RFB"), Rank = context.Ranks.Single(r => r.Description == "Firefighter"), IsActive = true },
                new Employee() { Id = 214874, Name = "James McCutcheon", Brigade = context.Brigades.Single(b => b.Name == "Davidson RFB"), Rank = context.Ranks.Single(r => r.Description == "Firefighter"), IsActive = true },
                new Employee() { Id = 95478, Name = "Tim Eliot", Brigade = context.Brigades.Single(b => b.Name == "Davidson RFB"), Rank = context.Ranks.Single(r => r.Description == "Deputy Captain"), IsActive = true }
            };
            employees.ForEach(e => context.Employees.Add(e));
            context.SaveChanges();
        }
    }
}
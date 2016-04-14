using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.DAL
{
    public class AttendanceContext : DbContext
    {
        public AttendanceContext() : base("AttendanceContext")
        {

        }

        public DbSet<UserTokenCache> UserTokenCacheList { get; set; }

        public DbSet<Models.Attendance> Attendances { get; set; }
        public DbSet<Brigade> Brigades { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Rank> Ranks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
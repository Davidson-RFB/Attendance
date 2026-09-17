#nullable disable

using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.DAL
{
    /// <summary>
    /// EF6 DbContext, kept as close to the original Framework 4.8 implementation
    /// as possible for schema/behavior parity (see docs/DOTNET10_MIGRATION_PLAN.md,
    /// section 2 - "Keep EF6", "100% database parity").
    ///
    /// The only structural difference from the original is how the connection
    /// string is supplied: EF6's classic `base("AttendanceContext")` constructor
    /// resolves a *name* against a <connectionStrings> section in app/web.config,
    /// which does not exist in ASP.NET Core. Instead, the actual connection
    /// string value (resolved from appsettings.json/environment via
    /// IConfiguration) is passed in directly - see the DI registration in
    /// Program.cs.
    /// </summary>
    [DbConfigurationType(typeof(AttendanceDbConfiguration))]
    public class AttendanceContext : DbContext
    {
        public AttendanceContext(string connectionString) : base(connectionString)
        {
        }

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

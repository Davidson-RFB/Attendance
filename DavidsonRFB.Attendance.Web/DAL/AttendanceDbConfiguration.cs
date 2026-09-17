using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;

namespace DavidsonRFB.Attendance.Web.DAL
{
    /// <summary>
    /// EF6 code-based configuration, replacing the classic &lt;entityFramework&gt;
    /// section of Web.config (which has no equivalent in ASP.NET Core / appsettings.json).
    ///
    /// This registers the SQL Server provider services/factory explicitly, since
    /// .NET (Core)/.NET 5+ does not populate System.Data.Common.DbProviderFactories
    /// from machine.config the way .NET Framework does - see the matching
    /// DbProviderFactories.RegisterFactory call in Program.cs.
    /// </summary>
    public class AttendanceDbConfiguration : DbConfiguration
    {
        public const string ProviderInvariantName = "System.Data.SqlClient";

        public AttendanceDbConfiguration()
        {
            SetProviderServices(ProviderInvariantName, SqlProviderServices.Instance);
            SetProviderFactory(ProviderInvariantName, SqlClientFactory.Instance);
            SetDefaultConnectionFactory(new SqlConnectionFactory());

            // NOTE: Database.SetInitializer<AttendanceContext>(null) is set from
            // Program.cs at startup, not here - calling it from inside this
            // constructor re-enters DbConfigurationManager while it's still
            // being constructed and throws.
        }
    }
}

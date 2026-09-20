using DavidsonRFB.Attendance.Web.DAL;

namespace DavidsonRFB.Attendance.Web.Tests.Infrastructure;

/// <summary>
/// Direct (non-HTTP) access to the same local SQL Server container the
/// CustomWebApplicationFactory-hosted app uses, for test setup/cleanup that
/// doesn't need to go through the app's own HTTP surface (e.g. deleting rows
/// created by a test so the shared dev DB stays reusable across runs).
/// </summary>
internal static class TestDb
{
    public const string ConnectionString =
        "Server=localhost,14330;Database=Attendance;User Id=sa;Password=LocalDev_Passw0rd!;TrustServerCertificate=True";

    public static AttendanceContext CreateContext() => new(ConnectionString);
}

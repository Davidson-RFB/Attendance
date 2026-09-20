using System.Data.Entity;
using System.Linq;
using System.Net;
using DavidsonRFB.Attendance.Web.Tests.Infrastructure;

namespace DavidsonRFB.Attendance.Web.Tests;

/// <summary>
/// The core kiosk workflow: clock a member in (POST Home/Index), see them on
/// the "currently clocked in" board, then clock them out (POST
/// Home/ClockOut) - against the real seeded local DB, via the real AAD
/// session (Home/Index+ClockOut also accept the Kiosk cookie per the OR
/// semantics, covered separately in AuthenticationTests).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class HomeClockInOutTests
{
    private readonly CustomWebApplicationFactory _factory;

    public HomeClockInOutTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ClockIn_ThenClockOut_RoundTrip()
    {
        var (employeeId, jobId) = GetSeededEmployeeAndJob();
        EnsureEmployeeNotAlreadyClockedIn(employeeId);

        var client = await _factory.CreateAadAuthenticatedClientAsync();

        // Act: clock in.
        var clockInForm = new Dictionary<string, string>
        {
            ["EmployeeId"] = employeeId.ToString(),
            ["JobId"] = jobId.ToString(),
        };
        var clockInResponse = await client.PostAsync("/", new FormUrlEncodedContent(clockInForm));
        var clockInBody = await clockInResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, clockInResponse.StatusCode);
        Assert.Contains("has been clocked in", clockInBody);

        int attendanceId;
        try
        {
            using var db = TestDb.CreateContext();
            var openAttendance = db.Attendances.Single(a => a.EmployeeId == employeeId && !a.EndDateTime.HasValue);
            attendanceId = openAttendance.Id;
            Assert.Null(openAttendance.EndDateTime);

            // Act: clock out via the ClockOut form (field names match
            // Views/Shared/EditorTemplates/Attendance.cshtml's
            // CurrentAttendance[i].Id / .Selected list-bound fields).
            var clockOutForm = new Dictionary<string, string>
            {
                ["CurrentAttendance[0].Id"] = attendanceId.ToString(),
                ["CurrentAttendance[0].Selected"] = "true",
            };
            // client follows redirects by default, so a successful ClockOut
            // (302 -> Index) surfaces here as the final 200 Index page.
            var clockOutResponse = await client.PostAsync("/Home/ClockOut", new FormUrlEncodedContent(clockOutForm));
            Assert.Equal(HttpStatusCode.OK, clockOutResponse.StatusCode);

            db.Entry(openAttendance).Reload();
            Assert.NotNull(openAttendance.EndDateTime);
        }
        finally
        {
            using var cleanup = TestDb.CreateContext();
            var toDelete = cleanup.Attendances.Where(a => a.EmployeeId == employeeId && a.JobId == jobId).ToList();
            foreach (var row in toDelete)
            {
                cleanup.Attendances.Remove(row);
            }
            cleanup.SaveChanges();
        }
    }

    private static (int EmployeeId, int JobId) GetSeededEmployeeAndJob()
    {
        using var db = TestDb.CreateContext();
        var employee = db.Employees.First(e => e.Email == CustomWebApplicationFactory.TestEmail);
        var job = db.Jobs.First(j => j.BrigadeId == employee.BrigadeId);
        return (employee.Id, job.Id);
    }

    /// <summary>
    /// Defensive cleanup in case a previous failed test run left the test
    /// employee "clocked in" - keeps this test idempotent/rerunnable against
    /// the shared dev DB.
    /// </summary>
    private static void EnsureEmployeeNotAlreadyClockedIn(int employeeId)
    {
        using var db = TestDb.CreateContext();
        var stale = db.Attendances.Where(a => a.EmployeeId == employeeId && !a.EndDateTime.HasValue).ToList();
        if (stale.Count == 0)
        {
            return;
        }

        foreach (var row in stale)
        {
            db.Attendances.Remove(row);
        }
        db.SaveChanges();
    }
}

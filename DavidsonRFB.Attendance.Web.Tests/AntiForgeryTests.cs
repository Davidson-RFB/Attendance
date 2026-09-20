using System.Data.Entity;
using System.Linq;
using System.Net;
using DavidsonRFB.Attendance.Web.Tests.Infrastructure;

namespace DavidsonRFB.Attendance.Web.Tests;

/// <summary>
/// Verifies the Phase 3 anti-forgery fix (docs/DOTNET10_MIGRATION_PLAN.md -
/// the legacy app had [ValidateAntiForgeryToken]/Html.AntiForgeryToken()
/// inconsistently commented out; the owner directed "fix it" rather than
/// preserve the gap). Every mutating Employee/Attendance action must both
/// require a valid token AND actually work when one is supplied.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AntiForgeryTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AntiForgeryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EmployeeCreate_WithoutAntiForgeryToken_IsRejected()
    {
        var client = await _factory.CreateAadAuthenticatedClientAsync();
        // A real page visit, so a valid antiforgery cookie IS present - only
        // the form field is withheld, isolating exactly what's under test.
        await client.GetAsync("/Employee/Create");

        var employeeId = NextTestEmployeeId();
        var form = new Dictionary<string, string>
        {
            ["Id"] = employeeId.ToString(),
            ["Name"] = "Test NoToken",
            ["Email"] = $"test.notoken.{employeeId}@example.com",
            ["IsActive"] = "true",
        };

        var response = await client.PostAsync("/Employee/Create", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var db = TestDb.CreateContext();
        Assert.Null(db.Employees.Find(employeeId));
    }

    [Fact]
    public async Task EmployeeCreate_WithAntiForgeryToken_Succeeds()
    {
        var client = await _factory.CreateAadAuthenticatedClientAsync();
        var createPage = await client.GetAsync("/Employee/Create");
        var token = await createPage.ExtractAntiForgeryTokenAsync();

        var employeeId = NextTestEmployeeId();
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = employeeId.ToString(),
            ["Name"] = "Test WithToken",
            ["Email"] = $"test.withtoken.{employeeId}@example.com",
            ["IsActive"] = "true",
        };

        try
        {
            // client follows redirects by default, so a successful Create
            // (302 -> Index) surfaces here as the final 200 Index page.
            var response = await client.PostAsync("/Employee/Create", new FormUrlEncodedContent(form));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Got {response.StatusCode}. Body: {body}");
            Assert.Contains("Test WithToken", body);

            using var db = TestDb.CreateContext();
            var created = db.Employees.Find(employeeId);
            Assert.NotNull(created);
            Assert.Equal("Test WithToken", created!.Name);
        }
        finally
        {
            using var cleanup = TestDb.CreateContext();
            var toDelete = cleanup.Employees.Find(employeeId);
            if (toDelete != null)
            {
                cleanup.Employees.Remove(toDelete);
                cleanup.SaveChanges();
            }
        }
    }

    [Fact]
    public async Task AttendanceCreate_WithoutAntiForgeryToken_IsRejected()
    {
        var client = await _factory.CreateAadAuthenticatedClientAsync();
        await client.GetAsync("/Attendance/Create");

        var (employeeId, jobId) = GetSeededEmployeeAndJob();
        var form = new Dictionary<string, string>
        {
            ["EmployeeId"] = employeeId.ToString(),
            ["JobId"] = jobId.ToString(),
            ["StartDateTime"] = DateTime.Now.ToString("o"),
        };

        var response = await client.PostAsync("/Attendance/Create", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AttendanceCreate_WithAntiForgeryToken_Succeeds()
    {
        var client = await _factory.CreateAadAuthenticatedClientAsync();
        var createPage = await client.GetAsync("/Attendance/Create");
        var token = await createPage.ExtractAntiForgeryTokenAsync();

        var (employeeId, jobId) = GetSeededEmployeeAndJob();
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["EmployeeId"] = employeeId.ToString(),
            ["JobId"] = jobId.ToString(),
            ["StartDateTime"] = DateTime.Now.ToString("o"),
        };

        int? createdId = null;
        try
        {
            // client follows redirects by default, so a successful Create
            // (302 -> Index) surfaces here as the final 200 Index page.
            var response = await client.PostAsync("/Attendance/Create", new FormUrlEncodedContent(form));
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Got {response.StatusCode}. Body: {body}");

            using var db = TestDb.CreateContext();
            var created = db.Attendances
                .Where(a => a.EmployeeId == employeeId && a.JobId == jobId)
                .OrderByDescending(a => a.Id)
                .FirstOrDefault();
            Assert.True(created != null, $"No Attendance row found for EmployeeId={employeeId}, JobId={jobId} after Create. Response body:\n{body}");
            createdId = created!.Id;
        }
        finally
        {
            if (createdId.HasValue)
            {
                using var cleanup = TestDb.CreateContext();
                var toDelete = cleanup.Attendances.Find(createdId.Value);
                if (toDelete != null)
                {
                    cleanup.Attendances.Remove(toDelete);
                    cleanup.SaveChanges();
                }
            }
        }
    }

    private static (int EmployeeId, int JobId) GetSeededEmployeeAndJob()
    {
        using var db = TestDb.CreateContext();
        var employee = db.Employees.First(e => e.Email == CustomWebApplicationFactory.TestEmail);
        var job = db.Jobs.First(j => j.BrigadeId == employee.BrigadeId);
        return (employee.Id, job.Id);
    }

    // Seeded employees (db/seed/02-seed-data.sql) use ids in the ~1,000-220,000
    // range; well-clear-of-seed-data test ids, offset by a per-process random
    // component so concurrent `dotnet test` runs (e.g. local + CI) don't collide.
    private static int NextTestEmployeeId() =>
        900_000 + Random.Shared.Next(0, 99_999);
}

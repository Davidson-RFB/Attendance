namespace DavidsonRFB.Attendance.Web.Tests.Infrastructure;

/// <summary>
/// All integration tests share one CustomWebApplicationFactory (one app
/// host, built once) and run sequentially, not in parallel - they exercise a
/// real, shared local SQL Server database (see scripts/db-up.sh) and a
/// shared seeded employee (Jason King) for AAD-identity impersonation via
/// FakeAadSignIn, so concurrent runs could race against each other's setup/
/// cleanup and produce flaky failures unrelated to the app itself.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}

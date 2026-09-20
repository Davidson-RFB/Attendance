using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DavidsonRFB.Attendance.Web.Tests.Infrastructure;

/// <summary>
/// In-process test host for the real DavidsonRFB.Attendance.Web app
/// (Program.cs is untouched - see FakeAadSignIn for how identity is faked).
///
/// Points ConnectionStrings:AttendanceContext at the real, already-seeded
/// local SQL Server container started by scripts/db-up.sh (see
/// docs/DOTNET10_MIGRATION_PLAN.md - local dev SQL container, port 14330) -
/// these tests exercise the real EF6 data layer against a real database, not
/// a mock/in-memory substitute (consistent with this migration's general
/// "verify for real" approach).
///
/// AzureAd:Instance/TenantId are set to the public "common" alias - its
/// OIDC discovery document (https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration)
/// is a stable, always-resolvable public endpoint, so the
/// "unauthenticated request challenges to a genuine AAD URL" test doesn't
/// depend on this repo's real (gitignored) tenant/client id in .env.
///
/// **Config is applied via real process environment variables, not
/// WebApplicationFactory's ConfigureAppConfiguration.** Found via live
/// testing: Program.cs's DataProtection:KeyRingPath (and, less critically,
/// AzureAd's options) are read directly off `builder.Configuration` as
/// inline top-level statements *before* `builder.Build()` runs.
/// WebApplicationFactory only gets a chance to splice its own
/// ConfigureAppConfiguration/ConfigureServices callbacks in right at the
/// Build() boundary (this is how it supports minimal-hosting Program.cs at
/// all) - by then, any config value already read via a plain
/// `builder.Configuration["..."]` indexer is already baked in and cannot be
/// changed. Real environment variables have no such timing problem: they're
/// visible to `WebApplication.CreateBuilder(args)`'s default configuration
/// sources from the very first line of Program.cs, since they're read from
/// the actual OS process environment, not anything WebApplicationFactory
/// injects later. Confirmed by first reproducing a real
/// `UnauthorizedAccessException`/`CryptographicException` writing to the
/// production "/keys" path when only ConfigureAppConfiguration was used.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestEmail = "jason.king@davidsonrfb.org";
    public const string TestEmployeeName = "Jason King";

    private readonly string _dataProtectionKeysPath =
        Path.Combine(Path.GetTempPath(), "davidsonrfb-attendance-test-keys-" + Guid.NewGuid());

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__AttendanceContext",
            "Server=localhost,14330;Database=Attendance;User Id=sa;Password=LocalDev_Passw0rd!;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("AzureAd__Instance", "https://login.microsoftonline.com/");
        Environment.SetEnvironmentVariable("AzureAd__TenantId", "common");
        Environment.SetEnvironmentVariable("AzureAd__ClientId", "00000000-0000-0000-0000-000000000000");
        Environment.SetEnvironmentVariable("AzureAd__CallbackPath", "/signin-oidc");
        Environment.SetEnvironmentVariable("DataProtection__KeyRingPath", _dataProtectionKeysPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, FakeAadSignInStartupFilter>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_dataProtectionKeysPath))
        {
            Directory.Delete(_dataProtectionKeysPath, recursive: true);
        }
    }
}

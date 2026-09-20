using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using DavidsonRFB.Attendance.Web.Auth;
using DavidsonRFB.Attendance.Web.DAL;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Identity.Web;

// .NET (Core)/.NET 5+ does not populate DbProviderFactories from
// machine.config the way .NET Framework does, so EF6 needs this registered
// explicitly before the first AttendanceContext is used. Must match the
// invariant name used in AttendanceDbConfiguration.
DbProviderFactories.RegisterFactory(AttendanceDbConfiguration.ProviderInvariantName, SqlClientFactory.Instance);

// The app explicitly controls schema (see AttendanceContext / db/seed/*.sql) -
// EF6 must never try to create or migrate the database itself, in dev or
// production. See docs/DOTNET10_MIGRATION_PLAN.md ("100% database parity").
Database.SetInitializer<AttendanceContext>(null);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("AttendanceContext");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'AttendanceContext' is not configured. Set it via " +
            "appsettings.json, an environment variable (ConnectionStrings__AttendanceContext), " +
            "or a user secret.");
    }

    return new AttendanceContext(connectionString);
});

// --- Authentication -----------------------------------------------------
// Two schemes: the AAD (Entra ID) session, set up by Microsoft.Identity.Web
// (this registers both the "OpenIdConnect" challenge scheme and the
// "Cookies" scheme that holds the resulting session), and a second, entirely
// separate "Kiosk" cookie scheme for the long-lived kiosk bypass. See
// docs/DOTNET10_MIGRATION_PLAN.md section 4 for the full design intent.
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// AddMicrosoftIdentityWebApp's "Cookies" scheme is only meant to be
// challenged indirectly (via the "OpenIdConnect" default challenge scheme).
// Our Home/ClockOut actions explicitly name both "Kiosk" and "Cookies" in
// their [Authorize(AuthenticationSchemes = ...)] though (see
// KioskAuthentication.HomeAndKioskSchemes), which means ASP.NET Core will
// challenge "Cookies" directly by name if both schemes fail to authenticate -
// without this, that falls back to its unconfigured default login path
// ("/Account/Login", which doesn't exist) and clobbers the proper AAD
// redirect. Forward it the same way as the Kiosk scheme below.
builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        return context.HttpContext.ChallengeAsync(
            OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = context.RedirectUri });
    };
});

builder.Services.AddAuthentication()
    .AddCookie(KioskAuthentication.Scheme, options =>
    {
        options.Cookie.Name = KioskAuthentication.CookieName;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(365 * 10);
        options.SlidingExpiration = true;

        // The Kiosk scheme has no login page of its own - if it's not
        // present/valid, fall through to an AAD sign-in challenge instead of
        // the default (nonexistent) cookie login path.
        options.Events.OnRedirectToLogin = context =>
        {
            return context.HttpContext.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = context.RedirectUri });
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Persist Data Protection keys (which the Kiosk cookie's ticket encryption
// depends on) to a mounted volume rather than the container's ephemeral
// filesystem default, so the 10-year kiosk cookie actually survives
// container restarts/redeploys. See Dockerfile/docker-compose.yml.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeyRingPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    var resolvedKeysPath = Path.IsPathRooted(dataProtectionKeysPath)
        ? dataProtectionKeysPath
        : Path.Combine(builder.Environment.ContentRootPath, dataProtectionKeysPath);

    builder.Services.AddDataProtection()
        .SetApplicationName("DavidsonRFB.Attendance")
        .PersistKeysToFileSystem(new DirectoryInfo(resolvedKeysPath));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Ported from the legacy app's IIS-level <httpErrors> 403 rewrite to
// /Error/Unauthorized - see Controllers/ErrorController.cs for why this is
// no longer [Authorize]-gated like the original.
app.UseStatusCodePagesWithReExecute("/Error/StatusCode/{0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Marker for WebApplicationFactory<Program>-based integration tests (a
// top-level-statements Program.cs generates an internal Program class by
// default - this makes it accessible). See DavidsonRFB.Attendance.Web.Tests.
public partial class Program { }

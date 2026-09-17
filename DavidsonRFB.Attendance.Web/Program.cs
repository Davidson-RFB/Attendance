using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using DavidsonRFB.Attendance.Web.DAL;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

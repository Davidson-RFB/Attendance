using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DavidsonRFB.Attendance.Web.Tests.Infrastructure;

/// <summary>
/// Test-only stand-in for an interactive Entra ID (AAD) sign-in, used by
/// <see cref="CustomWebApplicationFactory"/>. There is no way to complete a
/// real browser-based OIDC round trip in an automated test run, so this
/// fakes only the *identity establishment* step (normally done by a redirect
/// to login.microsoftonline.com) - everything downstream of that is 100%
/// real ASP.NET Core machinery: it calls the real
/// <c>HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, ...)</c>,
/// which mints a real, Data-Protection-encrypted "Cookies" scheme session
/// cookie via the exact same cookie handler <c>AddMicrosoftIdentityWebApp</c>
/// registers in Program.cs. No production code is touched to make this
/// work - it's wired in purely via an <see cref="IStartupFilter"/> added in
/// <see cref="CustomWebApplicationFactory.ConfigureWebHost"/>.
///
/// GET /__test/signin?email=...&amp;name=... signs the caller in with a
/// ClaimsPrincipal carrying `preferred_username` (email) and
/// <see cref="ClaimTypes.Name"/> (name) claims - matching the claims
/// HomeController.CreateCookie/CurrentBrigade.Resolve actually read from a
/// real AAD sign-in.
/// </summary>
internal static class FakeAadSignIn
{
    public const string Path = "/__test/signin";

    public static void MapEndpoint(IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals(Path, StringComparison.OrdinalIgnoreCase))
            {
                var email = context.Request.Query["email"].ToString();
                var name = context.Request.Query["name"].ToString();

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim("preferred_username", email));
                identity.AddClaim(new Claim(ClaimTypes.Name, string.IsNullOrEmpty(name) ? email : name));

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = false });

                context.Response.StatusCode = StatusCodes.Status200OK;
                return;
            }

            await next(context);
        });
    }
}

/// <summary>
/// Inserts <see cref="FakeAadSignIn"/>'s middleware at the very front of the
/// pipeline (before UseAuthentication/UseAuthorization), test-project-only.
/// </summary>
internal sealed class FakeAadSignInStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        FakeAadSignIn.MapEndpoint(app);
        next(app);
    };
}

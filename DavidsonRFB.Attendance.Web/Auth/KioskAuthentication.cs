namespace DavidsonRFB.Attendance.Web.Auth
{
    /// <summary>
    /// Constants for the "Kiosk" cookie authentication scheme - the ASP.NET
    /// Core replacement for the old FormsAuthentication-ticket kiosk cookie.
    ///
    /// Design intent (see docs/DOTNET10_MIGRATION_PLAN.md, section 4):
    ///   - Home/Index and ClockOut accept EITHER this scheme OR the Entra ID
    ///     (AAD) session scheme - see the [Authorize(AuthenticationSchemes = ...)]
    ///     usages on HomeController.
    ///   - Only HomeController.CreateCookie/RemoveCookie (both [Authorize],
    ///     i.e. AAD-only) may mint or revoke this cookie.
    ///   - Every other page requires the AAD scheme only - this cookie must
    ///     never grant access beyond Home/ClockOut.
    /// </summary>
    public static class KioskAuthentication
    {
        public const string Scheme = "Kiosk";
        public const string CookieName = "DavidsonRFB.Kiosk";

        /// <summary>
        /// Combined scheme list for [Authorize(AuthenticationSchemes = ...)] on
        /// the Home/ClockOut actions: succeeds if EITHER the Kiosk cookie or the
        /// AAD session cookie authenticates the request.
        /// </summary>
        public const string HomeAndKioskSchemes = Scheme + "," + Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;

        /// <summary>
        /// The AAD session scheme alone, for pages that must be AAD-only
        /// (Employee, Attendance maintenance, CreateCookie/RemoveCookie) -
        /// see docs/DOTNET10_MIGRATION_PLAN.md section 4.
        ///
        /// Must be specified explicitly rather than relying on bare
        /// [Authorize]'s default authenticate scheme: this app registers
        /// authentication via `AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        /// .AddMicrosoftIdentityWebApp(...)`, which leaves
        /// AuthenticationOptions.DefaultAuthenticateScheme as "OpenIdConnect"
        /// - a challenge-only handler that can never authenticate an
        /// incoming request's existing "Cookies" session on its own. Bare
        /// [Authorize] under this registration therefore always reports
        /// "not authenticated" and always challenges, regardless of whether
        /// a valid AAD cookie is present. Caught via live integration
        /// testing (see DavidsonRFB.Attendance.Web.Tests) - a real signed-in
        /// session could never actually reach Employee/Attendance, and
        /// CreateCookie/RemoveCookie could never be invoked by a real signed-in
        /// user either, making the kiosk cookie mechanism entirely
        /// un-provisionable in production. Fixed by naming this scheme
        /// explicitly everywhere AAD-only access is required, rather than
        /// relying on framework defaults.
        /// </summary>
        public const string AadOnlyScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;

        /// <summary>
        /// Claim carrying the BrigadeId, set when HomeController.CreateCookie
        /// signs a kiosk in. Replaces the old FormsAuthenticationTicket.Name
        /// (which stored the brigade id as a bare string).
        /// </summary>
        public const string BrigadeIdClaimType = "davidsonrfb:brigade_id";
    }
}

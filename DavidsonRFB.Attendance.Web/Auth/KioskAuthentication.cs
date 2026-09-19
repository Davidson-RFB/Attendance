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
        /// Claim carrying the BrigadeId, set when HomeController.CreateCookie
        /// signs a kiosk in. Replaces the old FormsAuthenticationTicket.Name
        /// (which stored the brigade id as a bare string).
        /// </summary>
        public const string BrigadeIdClaimType = "davidsonrfb:brigade_id";
    }
}

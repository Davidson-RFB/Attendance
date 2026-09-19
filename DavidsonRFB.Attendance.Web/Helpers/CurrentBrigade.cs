using System.Globalization;
using System.Linq;
using System.Security.Claims;
using DavidsonRFB.Attendance.Web.Auth;
using DavidsonRFB.Attendance.Web.DAL;

namespace DavidsonRFB.Attendance.Web.Helpers
{
    /// <summary>
    /// Replacement for the legacy Helpers/Brigade.cs. Resolves which brigade
    /// the current request should be scoped to, regardless of whether it
    /// authenticated via the AAD session or the Kiosk cookie.
    ///
    /// The old implementation read HttpContext.Current.User/Request.Cookies
    /// directly (relying on System.Web statics that don't exist in ASP.NET
    /// Core); this takes the ClaimsPrincipal and AttendanceContext explicitly
    /// instead, which is both idiomatic DI-friendly ASP.NET Core and easier to
    /// unit test.
    /// </summary>
    public static class CurrentBrigade
    {
        public static int? Resolve(ClaimsPrincipal user, AttendanceContext db)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // Authenticated via the Kiosk cookie: the brigade id is a claim set
            // by HomeController.CreateCookie, not looked up from an employee
            // record (there is no signed-in employee on a kiosk session).
            var kioskBrigadeIdClaim = user.FindFirst(KioskAuthentication.BrigadeIdClaimType);
            if (kioskBrigadeIdClaim != null &&
                int.TryParse(kioskBrigadeIdClaim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var kioskBrigadeId))
            {
                return kioskBrigadeId;
            }

            // Otherwise, authenticated via AAD: resolve via the signed-in
            // user's employee record, matched on their AAD preferred_username
            // (their email/UPN), same as the original app matched on
            // User.Identity.Name.
            var email = user.FindFirst("preferred_username")?.Value ?? user.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            return db.Employees
                .Where(e => e.Email == email)
                .Select(e => (int?)e.BrigadeId)
                .SingleOrDefault();
        }
    }
}

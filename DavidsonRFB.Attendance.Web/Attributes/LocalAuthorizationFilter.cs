using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DavidsonRFB.Attendance.Web.DAL;

namespace DavidsonRFB.Attendance.Web.Filters
{
    public class LocalAuthorization : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Check for a local cookie
            HttpCookie authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null && !string.IsNullOrEmpty(authCookie.Value))
            {
                FormsAuthenticationTicket authTicket;
                try
                {
                    authTicket = FormsAuthentication.Decrypt(authCookie.Value);

                    AttendanceContext context = new AttendanceContext();
                    if (context.Brigades.Any(b => b.Id.ToString() == authTicket.Name) && authTicket.UserData.Contains("Log Time"))
                    {
                        return true;
                    }
                }
                catch { }
            }

            return base.AuthorizeCore(httpContext);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Return a "403 Forbidden" status rather than redirecting to the login page with the standard "401 Unauthorized"
            filterContext.Result = new HttpStatusCodeResult((int)System.Net.HttpStatusCode.Forbidden);
        }
    }
}
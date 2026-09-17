using System.Linq;
using System.Web;
using System.Web.Security;
using DavidsonRFB.Attendance.Web.Models;

namespace DavidsonRFB.Attendance.Web.Helpers
{
    public static class Brigade
    {
        public static int? CurrentBrigade()
        {
            int? brigadeId = null;
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                DAL.AttendanceContext context = new DAL.AttendanceContext();
                Employee employee = context.Employees.Single(e => e.Email == HttpContext.Current.User.Identity.Name);
                if (employee != null)
                {
                    return employee.BrigadeId;
                } 
            }
            else
            {
                HttpCookie authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie != null && !string.IsNullOrEmpty(authCookie.Value))
                {
                    FormsAuthenticationTicket authTicket;
                    try
                    {
                        authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                        return int.Parse(authTicket.Name);
                    }
                    catch { }
                }
            }

            return brigadeId;
        }
    }
}
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DavidsonRFB.Attendance.Web.Startup))]
namespace DavidsonRFB.Attendance.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

using Microsoft.Owin;
using Owin;

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

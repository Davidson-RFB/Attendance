using System.Web.Mvc;

namespace DavidsonRFB.Attendance.Web.Controllers
{
    [Authorize]
    public class ErrorController : Controller
    {
        [HttpGet]
        public ActionResult Unauthorized()
        {
            return View();
        }
    }
}
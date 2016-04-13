using System.Web.Mvc;

namespace DavidsonRFB.Attendance.Web.Controllers
{
    public class ErrorController : Controller
    {
        [HttpGet]
        public ActionResult Unauthorized()
        {
            return View();
        }
    }
}
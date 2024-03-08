using Microsoft.AspNetCore.Mvc;

namespace Simulysis.Controllers
{
    public class ProjectController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

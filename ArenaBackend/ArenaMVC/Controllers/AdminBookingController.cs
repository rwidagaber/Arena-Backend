using Microsoft.AspNetCore.Mvc;

namespace ArenaMVC.Controllers
{
    public class AdminBookingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

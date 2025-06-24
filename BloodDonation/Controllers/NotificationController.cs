using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    public class NotificationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

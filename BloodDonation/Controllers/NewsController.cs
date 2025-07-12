using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Controllers
{
    public class NewsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
} 
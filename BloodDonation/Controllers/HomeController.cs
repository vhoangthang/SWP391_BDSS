using System.Diagnostics;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Data;
using Microsoft.EntityFrameworkCore;

namespace BloodDonation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(username);
            ViewBag.Username = username;

            int unreadCount = 0;
            if (!string.IsNullOrEmpty(username))
            {
                var donor = _context.Donors
                    .Include(d => d.Account)
                    .FirstOrDefault(d => d.Account.Username == username);
                if (donor != null)
                {
                    unreadCount = _context.Notifications.Count(n => n.AccountID == donor.AccountID && !n.IsRead);
                }
            }
            ViewBag.UnreadNotificationCount = unreadCount;

            string userDisplayName = "Người mới";
            if (!string.IsNullOrEmpty(username))
            {
                var donor = _context.Donors
                    .Include(d => d.Account)
                    .FirstOrDefault(d => d.Account.Username == username);
                if (donor != null && !string.IsNullOrWhiteSpace(donor.Name))
                {
                    userDisplayName = donor.Name;
                }
            }
            ViewBag.UserDisplayName = userDisplayName;


            // Get TeamMember News with Type = "blogs"
            var teamMembers = _context.News
                .Where(n => n.Type == "blogs")
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
            ViewBag.TeamMembers = teamMembers;

            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

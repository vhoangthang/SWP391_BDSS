using Microsoft.AspNetCore.Mvc;
using BloodDonation.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;

namespace BloodDonation.Controllers
{
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;
        public NotificationController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var donor = _context.Donors.Include(d => d.Notifications).Include(d => d.Account).FirstOrDefault(d => d.Account.Username == username);
            if (donor == null)
                return RedirectToAction("Index", "Login");

            var notifications = donor.Notifications.OrderByDescending(n => n.SentAt).ToList();
            return View(notifications);
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var donor = _context.Donors.Include(d => d.Notifications).Include(d => d.Account).FirstOrDefault(d => d.Account.Username == username);
            if (donor == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng" });

            var notification = donor.Notifications.FirstOrDefault(n => n.NotificationID == id);
            if (notification == null)
                return Json(new { success = false, message = "Không tìm thấy thông báo" });

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }
            return Json(new { success = true });
        }
    }
}

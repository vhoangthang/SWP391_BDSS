
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BloodDonation.Data;

namespace BloodDonation.Controllers
{
    public class BookingDayAndBloodTypeController : Controller
    {
        private readonly AppDbContext _context;

        public BookingDayAndBloodTypeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(BookAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type");
                return View(model);
            }

            // Lưu thông tin vào session để chuyển sang form 2
            HttpContext.Session.SetString("AppointmentDate", model.AppointmentDate?.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetString("TimeSlot", model.TimeSlot);
            HttpContext.Session.SetInt32("BloodTypeID", model.BloodTypeID);

            return RedirectToAction("Index", "DonationForm");
        }
    }
}
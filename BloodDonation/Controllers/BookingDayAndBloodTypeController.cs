using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BloodDonation.Data;
using System.Linq;

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
            // Get username from session
            var username = HttpContext.Session.GetString("Username");
            int? userBloodTypeId = null;
            if (!string.IsNullOrEmpty(username))
            {
                var account = _context.Accounts.FirstOrDefault(a => a.Username == username);
                if (account != null)
                {
                    var donor = _context.Donors.FirstOrDefault(d => d.AccountID == account.AccountID);
                    if (donor != null)
                    {
                        userBloodTypeId = donor.BloodTypeID;
                    }
                }
            }
            ViewBag.UserBloodTypeID = userBloodTypeId;
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

            // Save in session
            HttpContext.Session.SetString("AppointmentDate", model.AppointmentDate?.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetString("TimeSlot", model.TimeSlot);
            HttpContext.Session.SetInt32("BloodTypeID", model.BloodTypeID);

            return RedirectToAction("Index", "DonationForm");
        }
    }
}
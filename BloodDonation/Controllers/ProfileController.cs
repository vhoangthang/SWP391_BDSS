using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

using BloodDonation.Models;


namespace BloodDonation.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var account = _context.Accounts.FirstOrDefault(a => a.Username == username);
            if (account == null)
                return NotFound();

            var donor = _context.Donors
                .Include(d => d.BloodType)
                .FirstOrDefault(d => d.AccountID == account.AccountID);
            if (donor == null)
            {
                ViewBag.Email = account.Email;
                ViewBag.ProfileMessage = "Chưa có hồ sơ, vui lòng cập nhật!";
                return View(new BloodDonation.Models.Donor { AccountID = account.AccountID });
            }

            ViewBag.Email = account.Email;
            return View(donor);
        }

        public IActionResult Edit()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var account = _context.Accounts.FirstOrDefault(a => a.Username == username);
            if (account == null)
                return NotFound();

            var donor = _context.Donors.FirstOrDefault(d => d.AccountID == account.AccountID);
            if (donor == null)
            {
                donor = new BloodDonation.Models.Donor { AccountID = account.AccountID };
            }

            // Check if user can edit IsAvailable: only allow if there is a pending or confirmed donation appointment
            var canEditIsAvailable = _context.DonationAppointments.Any(a => a.DonorID == donor.DonorID && (a.Status == "Pending" || a.Status == "Confirmed"));
            ViewBag.CanEditIsAvailable = canEditIsAvailable;

            ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type", donor.BloodTypeID);
            return View(donor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Donor donor)
        {
            ModelState.Remove("Account");
            ModelState.Remove("BloodType");
            ModelState.Remove("DonorBloodRequests");
            ModelState.Remove("DonationAppointments");

            if (!ModelState.IsValid)
            {
                ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type", donor.BloodTypeID);
                return View(donor);
            }

            var existingDonor = _context.Donors.FirstOrDefault(d => d.AccountID == donor.AccountID);
            if (existingDonor == null)
            {
                // Create new Donor
                _context.Donors.Add(donor);
            }
            else
            {
                // Update information
                existingDonor.Name = donor.Name;
                existingDonor.DateOfBirth = donor.DateOfBirth;
                existingDonor.Gender = donor.Gender;
                existingDonor.ContactNumber = donor.ContactNumber;
                existingDonor.Address = donor.Address;
                existingDonor.IsAvailable = donor.IsAvailable;
                existingDonor.CCCD = donor.CCCD;
                existingDonor.BloodTypeID = donor.BloodTypeID;
            }
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
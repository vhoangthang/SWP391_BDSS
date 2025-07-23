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
                var donorInDb = _context.Donors.FirstOrDefault(d => d.AccountID == donor.AccountID);
                bool canEditIsAvailable = false;
                if (donorInDb != null)
                {
                    canEditIsAvailable = _context.DonationAppointments.Any(a => a.DonorID == donorInDb.DonorID && (a.Status == "Pending" || a.Status == "Confirmed"));
                }
                ViewBag.CanEditIsAvailable = canEditIsAvailable;
                ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type", donor.BloodTypeID);
                return View(donor);
            }

            if (donor.DateOfBirth.HasValue)
            {
                var today = DateTime.Today;
                int age = today.Year - donor.DateOfBirth.Value.Year;
                if (donor.DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                if (age < 18 || age > 60)
                {
                    var donorInDb = _context.Donors.FirstOrDefault(d => d.AccountID == donor.AccountID);
                    bool canEditIsAvailable = false;
                    if (donorInDb != null)
                    {
                        canEditIsAvailable = _context.DonationAppointments.Any(a => a.DonorID == donorInDb.DonorID && (a.Status == "Pending" || a.Status == "Confirmed"));
                    }
                    ViewBag.CanEditIsAvailable = canEditIsAvailable;
                    ModelState.AddModelError("DateOfBirth", "Bạn phải từ 18 đến 60 tuổi mới có thể đăng ký hiến máu.");
                    ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type", donor.BloodTypeID);
                    return View(donor);
                }
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
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                var donorInDb = _context.Donors.FirstOrDefault(d => d.AccountID == donor.AccountID);
                bool canEditIsAvailable = false;
                if (donorInDb != null)
                {
                    canEditIsAvailable = _context.DonationAppointments.Any(a => a.DonorID == donorInDb.DonorID && (a.Status == "Pending" || a.Status == "Confirmed"));
                }
                ViewBag.CanEditIsAvailable = canEditIsAvailable;
                ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type", donor.BloodTypeID);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng kiểm tra lại và thử lại!");
                return View(donor);
            }
            return RedirectToAction("Index");
        }
    }
}
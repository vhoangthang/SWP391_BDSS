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
            // Lấy username từ session
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Tìm account theo username
            var account = _context.Accounts.FirstOrDefault(a => a.Username == username);
            if (account == null)
                return NotFound();

            // Tìm donor theo AccountID
            var donor = _context.Donors
                .Include(d  => d.BloodType)
                .FirstOrDefault(d => d.AccountID == account.AccountID);
            if (donor == null)
                return NotFound();

            // Kiểm tra có đơn nào KHÔNG phải Completed hoặc Rejected không
            var canEdit = _context.DonationAppointments.Any(a => a.DonorID == donor.DonorID && a.Status != "Completed" && a.Status != "Rejected");
            ViewBag.CanEditIsAvailable = canEdit;

            // Truyền email từ account vào ViewBag
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
                return NotFound();

            // Bổ sung: Kiểm tra có đơn nào KHÔNG phải Completed hoặc Rejected không
            var canEdit = _context.DonationAppointments.Any(a => a.DonorID == donor.DonorID && a.Status != "Completed" && a.Status != "Rejected");
            ViewBag.CanEditIsAvailable = canEdit;

            ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type", donor.BloodTypeID);
            return View(donor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Donor donor)
        {
            // Xóa lỗi validation cho các trường navigation
            ModelState.Remove("Account");
            ModelState.Remove("BloodType");
            ModelState.Remove("DonorBloodRequests");
            ModelState.Remove("DonationAppointments");

            if (!ModelState.IsValid)
            {
                ViewBag.BloodTypes = new SelectList(_context.BloodTypes.ToList(), "BloodTypeID", "Type", donor.BloodTypeID);
                return View(donor);
            }
                

            var existingDonor = _context.Donors.FirstOrDefault(d => d.DonorID == donor.DonorID);
            if (existingDonor == null)
                return NotFound();

            // Cập nhật thông tin
            existingDonor.Name = donor.Name;
            existingDonor.DateOfBirth = donor.DateOfBirth;
            existingDonor.Gender = donor.Gender;
            existingDonor.ContactNumber = donor.ContactNumber;
            existingDonor.Address = donor.Address;
            existingDonor.CCCD = donor.CCCD;
            existingDonor.BloodTypeID = donor.BloodTypeID;
            existingDonor.IsAvailable = donor.IsAvailable;

            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
} 
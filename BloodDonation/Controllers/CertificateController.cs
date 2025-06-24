using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BloodDonation.Controllers
{
    public class CertificateController : Controller
    {
        private readonly AppDbContext _context;

        public CertificateController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Login");
            }

            var donor = _context.Donors
                .Include(d => d.Account)
                .FirstOrDefault(d => d.Account.Username == username);

            if (donor == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin người hiến máu.";
                return RedirectToAction("Index", "Home");
            }
            
            var certificates = _context.DonationCertificates
                .Include(c => c.Appointment)
                    .ThenInclude(a => a.MedicalCenter)
                .Where(c => c.Appointment.DonorID == donor.DonorID)
                .OrderByDescending(c => c.IssueDate)
                .ToList();

            return View(certificates);
        }
    }
} 
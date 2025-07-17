using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

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

            List<DonationCertificate> certificates;

            if (donor == null)
            {
                ViewBag.NoCertificate = true;
                certificates = new List<DonationCertificate>();
            }
            else
            {
                certificates = _context.DonationCertificates
                    .Include(c => c.Appointment)
                        .ThenInclude(a => a.MedicalCenter)
                    .Where(c => c.Appointment.DonorID == donor.DonorID)
                    .OrderByDescending(c => c.IssueDate)
                    .ToList();
            }

            return View(certificates);
        }
    }
} 
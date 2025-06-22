using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BloodDonation.Models;
using System;
using System.Linq;

namespace BloodDonation.Controllers
{
    public class LoginController : Controller
    {
        private readonly MyDbContext _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(MyDbContext context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
            ViewBag.Username = HttpContext.Session.GetString("Username");
            return View();
        }
        public ActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa toàn bộ session
            return RedirectToAction("Index", "Home");
        }

        // POST: Login
        [HttpPost]
        public ActionResult Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var acc = _context.Accounts.FirstOrDefault(a =>
                    a.Username == model.Username &&
                    a.Password == model.Password);

                if (acc != null)
                {
                    HttpContext.Session.SetString("Username", acc.Username);
                    HttpContext.Session.SetString("Role", acc.Role);

                    _logger.LogInformation($"User logged in: Username={acc.Username}, Role={acc.Role}");

                    switch (acc.Role.ToLower())
                    {
                        case "donor":
                            return RedirectToAction("Index", "Home");
                        case "medicalcenter":
                            if (acc.MedicalCenterID.HasValue)
                            {
                                HttpContext.Session.SetInt32("MedicalCenterID", acc.MedicalCenterID.Value);
                                _logger.LogInformation($"MedicalCenterID set from Account: {acc.MedicalCenterID.Value}");
                            }
                            else
                            {
                                _logger.LogWarning($"Account {acc.Username} has role 'medicalcenter' but no MedicalCenterID assigned");
                                ModelState.AddModelError("", "Tài khoản chưa được liên kết với trung tâm y tế.");
                                return View(model);
                            }
                            return RedirectToAction("Index", "MedicalCenter");
                        case "staff":
                            return RedirectToAction("DonationList", "Staff");
                        case "admin":
                            return RedirectToAction("Index", "Admin");
                    }
                }

                ModelState.AddModelError("", "Sai thông tin đăng nhập.");
            }

            return View(model);
        }
    }
}

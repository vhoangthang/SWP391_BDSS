using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BloodDonation.Data;
using BloodDonation.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;



namespace BloodDonation.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                _logger.LogWarning($"Unauthorized access attempt: Username={username}, Role={role}");
                return RedirectToAction("Index", "Login");
            }

            var totalUsers = await _context.Accounts.CountAsync();
            var totalBloodRequests = await _context.BloodRequests.CountAsync();
            var totalBloodInventory = await _context.BloodInventories.SumAsync(bi => bi.Quantity);
            //var donorCount = await _context.Donors.CountAsync();

            var donationTrends = await _context.DonationAppointments
                .GroupBy(a => a.AppointmentDate.Month)
                .Select(g => new
                {
                    month = new DateTime(2025, g.Key, 1).ToString("MMM"),
                    donations = g.Count(a => a.Status == "Completed"),
                    requests = g.Count(a => a.Status == "Requested")
                }).ToListAsync();

            // Danh sách nhóm máu chuẩn
            // Danh sách 8 nhóm máu chuẩn
            var bloodTypes = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

            // Lấy số lượt hiến thành công theo nhóm máu
            var completedDonations = await _context.DonationAppointments
                .Where(a => a.Status == "Completed" && a.BloodType != null)
                .GroupBy(a => a.BloodType.Type)
                .Select(g => new
                {
                    bloodType = g.Key,
                    count = g.Count()
                }).ToListAsync();

            // Ghép đủ 8 nhóm máu, nhóm nào không có thì count = 0
            var bloodTypeDistribution = bloodTypes
                .Select(bt => new
                {
                    bloodType = bt,
                    count = completedDonations.FirstOrDefault(x => x.bloodType == bt)?.count ?? 0
                }).ToList();







            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalBloodRequests = totalBloodRequests;
            ViewBag.TotalBloodInventory = totalBloodInventory;
            //ViewBag.AvailableDonors = donorCount;
            ViewBag.DonationTrends = donationTrends;
            ViewBag.BloodTypeDistribution = bloodTypeDistribution;

            return View();
        }

        public async Task<IActionResult> DonationHistory()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            var donationHistory = await _context.DonationAppointments
                .Include(a => a.Donor)
                    .ThenInclude(d => d.Account)
                .Include(a => a.BloodType)        // ✅ Cần để hiển thị nhóm máu
                       
                .Where(a => a.Status == "Completed")
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(donationHistory);
        }


        public async Task<IActionResult> UserManagement()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            var users = await _context.Accounts
                .Include(a => a.MedicalCenter)
                .OrderBy(a => a.Username)
                .ToListAsync();

            var medicalCenters = await _context.MedicalCenters.ToListAsync();
            ViewBag.MedicalCenters = medicalCenters;

            return View(users);
        }

        public async Task<IActionResult> BloodRequestManagement()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            var bloodRequests = await _context.BloodRequests
                .Include(br => br.MedicalCenter)
                .Include(br => br.BloodType)
                .OrderByDescending(br => br.RequestDate)
                .ToListAsync();

            // Lấy danh sách nhóm máu còn trong kho
            var availableBloodTypes = await _context.BloodInventories
                .Include(b => b.BloodType)
                .Where(b => b.Quantity > 0)
                .Select(b => b.BloodType.Type)
                .ToListAsync();

            return View(bloodRequests);
        }

        public async Task<IActionResult> BloodInventoryManagement()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            var bloodInventory = await _context.BloodInventories
                .Include(bi => bi.BloodType)
                .Include(bi => bi.BloodBank)
                .OrderBy(bi => bi.BloodType.Type)
                .ToListAsync();

            return View(bloodInventory);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBloodRequestStatus(int requestId, string status)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var bloodRequest = await _context.BloodRequests.FindAsync(requestId);
                if (bloodRequest != null)
                {
                    bloodRequest.Status = status;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Cập nhật thành công" });
                }
                return Json(new { success = false, message = "Không tìm thấy yêu cầu" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating blood request status: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBloodInventory(int bloodTypeId, decimal quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                const int bloodBankId = 1;

                var bloodInventory = await _context.BloodInventories
                    .FirstOrDefaultAsync(i => i.BloodTypeID == bloodTypeId && i.BloodBankID == bloodBankId);

                if (bloodInventory != null)
                {
                    bloodInventory.Quantity = quantity;
                    bloodInventory.LastUpdated = DateTime.Now;
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Cập nhật kho máu thành công!";
                    return RedirectToAction("BloodInventoryManagement");
                }

                TempData["Error"] = "Không tìm thấy nhóm máu trong kho.";
                return RedirectToAction("BloodInventoryManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating blood inventory: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật kho máu.";
                return RedirectToAction("BloodInventoryManagement");
            }
        }

        // ✅ Hàm kiểm tra tương hợp nhóm máu
        private bool CheckCompatibility(string donor, string recipient)
        {
            if (donor == "O-") return true;
            if (donor == recipient) return true;
            if (donor == "O+" && (recipient == "A+" || recipient == "B+" || recipient == "AB+" || recipient == "O+")) return true;
            if (donor == "A-" && (recipient == "A+" || recipient == "A-" || recipient == "AB+" || recipient == "AB-")) return true;
            if (donor == "B-" && (recipient == "B+" || recipient == "B-" || recipient == "AB+" || recipient == "AB-")) return true;
            if (donor == "AB-" && (recipient == "AB+" || recipient == "AB-")) return true;
            if (donor == "A+" && (recipient == "A+" || recipient == "AB+")) return true;
            if (donor == "B+" && (recipient == "B+" || recipient == "AB+")) return true;
            if (donor == "AB+" && recipient == "AB+") return true;

            return false;
        }

        public class DeleteUserRequest
        {
            public int id { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequest req)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            // Xóa các appointment có Status hoặc HealthSurvey bị NULL trước khi truy vấn Donor
            var nullAppointments = _context.DonationAppointments
                .Where(a => a.Status == null || a.HealthSurvey == null)
                .ToList();
            if (nullAppointments.Any())
            {
                _context.DonationAppointments.RemoveRange(nullAppointments);
                await _context.SaveChangesAsync();
            }

            var user = await _context.Accounts.FindAsync(req.id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            if (user.Username == username || user.Role.ToLower() == "admin")
            {
                return Json(new { success = false, message = "Không thể xóa tài khoản admin hoặc chính bạn." });
            }

            // Xóa donor và các bản ghi liên quan nếu có
            var donor = await _context.Donors
                .Include(d => d.DonorBloodRequests)
                .Include(d => d.DonationAppointments)
                .Include(d => d.Notifications)
                .FirstOrDefaultAsync(d => d.AccountID == user.AccountID);

            if (donor != null)
            {
                if (donor.DonationAppointments != null)
                {
                    // Xóa các bản ghi liên quan đến các appointment còn lại
                    var validAppointments = donor.DonationAppointments
                        .Where(a => a.Status != null && a.HealthSurvey != null)
                        .ToList();
                    var appointmentIds = validAppointments.Select(a => a.AppointmentID).ToList();

                    // Xóa HealthSurvey liên quan
                    var surveys = _context.HealthSurveys.Where(s => appointmentIds.Contains(s.AppointmentID));
                    _context.HealthSurveys.RemoveRange(surveys);

                    // Xóa DonationCertificate liên quan
                    var certificates = _context.DonationCertificates.Where(c => appointmentIds.Contains(c.AppointmentID));
                    _context.DonationCertificates.RemoveRange(certificates);

                    // Xóa các appointment còn lại
                    _context.DonationAppointments.RemoveRange(validAppointments);
                }
                if (donor.DonorBloodRequests != null)
                    _context.DonorBloodRequests.RemoveRange(donor.DonorBloodRequests);
                if (donor.Notifications != null)
                    _context.Notifications.RemoveRange(donor.Notifications);

                _context.Donors.Remove(donor);
            }

            _context.Accounts.Remove(user);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa tài khoản thành công." });
        }

        public class ChangeRoleRequest
        {
            public int id { get; set; }
            public string newRole { get; set; }
            public int? medicalCenterId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleRequest req)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            var user = await _context.Accounts.FindAsync(req.id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản." });
            }

            // Không cho phép đổi role của chính mình
            if (user.Username == username)
            {
                return Json(new { success = false, message = "Không thể đổi vai trò của chính bạn." });
            }

            user.Role = req.newRole;
            if (req.newRole.ToLower() == "medicalcenter")
                user.MedicalCenterID = req.medicalCenterId;
            else
                user.MedicalCenterID = null;

            // Cập nhật PermissionLevel đúng với Role
            switch (req.newRole.ToLower())
            {
                case "donor":
                    user.PermissionLevel = 1;
                    break;
                case "medicalcenter":
                    user.PermissionLevel = 1;
                    break;
                case "staff":
                    user.PermissionLevel = 2;
                    break;
                case "admin":
                    user.PermissionLevel = 3;
                    break;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật vai trò thành công." });
        }


    }
}

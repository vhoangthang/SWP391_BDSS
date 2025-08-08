using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BloodDonation.Data;
using BloodDonation.Models;
using System;
using System.Linq;
using System.Threading.Tasks;



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
            var donationTrends = await _context.DonationAppointments
                .GroupBy(a => a.AppointmentDate.Month)
                .Select(g => new
                {
                    month = new DateTime(2025, g.Key, 1).ToString("MMM"),
                    donations = g.Count(a => a.Status == "Completed"),
                    requests = _context.BloodRequests
                        .Where(br => br.RequestDate.Month == g.Key && br.Status == "Completed")
                        .Count()
                }).ToListAsync();

            var bloodTypes = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
            var completedDonations = await _context.DonationAppointments
                .Where(a => a.Status == "Completed" && a.BloodType != null)
                .GroupBy(a => a.BloodType.Type)
                .Select(g => new
                {
                    bloodType = g.Key,
                    count = g.Count()
                }).ToListAsync();
            var bloodTypeDistribution = bloodTypes
                .Select(bt => new
                {
                    bloodType = bt,
                    count = completedDonations.FirstOrDefault(x => x.bloodType == bt)?.count ?? 0
                }).ToList();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalBloodRequests = totalBloodRequests;
            ViewBag.TotalBloodInventory = totalBloodInventory;
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
                .Include(a => a.BloodType)
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

            var allBloodTypes = await _context.BloodTypes.ToListAsync();
            ViewBag.AllBloodTypes = allBloodTypes;

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
        public async Task<IActionResult> UpdateBloodInventory(int bloodTypeId, decimal quantity, string operation)
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

                decimal change = operation == "subtract" ? -quantity : quantity;

                if (bloodInventory != null)
                {
                    bloodInventory.Quantity += change;
                    if (bloodInventory.Quantity < 0)
                        bloodInventory.Quantity = 0;

                    bloodInventory.LastUpdated = DateTime.Now;
                    _context.Update(bloodInventory);
                }
                else
                {
                    var newInventory = new BloodInventory
                    {
                        BloodTypeID = bloodTypeId,
                        BloodBankID = bloodBankId,
                        Quantity = change < 0 ? 0 : change,
                        LastUpdated = DateTime.Now
                    };
                    _context.Add(newInventory);
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = "Cập nhật kho máu thành công!";
                return RedirectToAction("BloodInventoryManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating blood inventory: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật kho máu.";
                return RedirectToAction("BloodInventoryManagement");
            }
        }

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

        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequest req)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            // Delete null appointment
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

            // Delete donor
            var donor = await _context.Donors
                .Include(d => d.DonorBloodRequests)
                .Include(d => d.DonationAppointments)
                .Include(d => d.Notifications)
                .FirstOrDefaultAsync(d => d.AccountID == user.AccountID);

            if (donor != null)
            {
                if (donor.DonationAppointments != null)
                {
                    // Delete appointment
                    var validAppointments = donor.DonationAppointments
                        .Where(a => a.Status != null && a.HealthSurvey != null)
                        .ToList();
                    var appointmentIds = validAppointments
                        .Select(a => a.AppointmentID)
                        .ToList();

                    // Delete healthsurvey
                    var surveys = _context.HealthSurveys
                        .Where(s => appointmentIds
                        .Contains(s.AppointmentID));
                    _context.HealthSurveys.RemoveRange(surveys);

                    // Delete certificate
                    var certificates = _context.DonationCertificates
                        .Where(c => appointmentIds
                        .Contains(c.AppointmentID));
                    _context.DonationCertificates.RemoveRange(certificates);

                    // Delete another appointment
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


            if (user.Username == username)
            {
                return Json(new { success = false, message = "Không thể đổi vai trò của chính bạn." });
            }

            user.Role = req.newRole;
            if (req.newRole.ToLower() == "medicalcenter")
                user.MedicalCenterID = req.medicalCenterId;
            else
                user.MedicalCenterID = null;

            // Update permission
            switch (req.newRole.ToLower())
            {
                case "donor":
                    user.Role = "Donor";
                    user.PermissionLevel = 1;
                    break;
                case "medicalcenter":
                    user.Role = "MedicalCenter";
                    user.PermissionLevel = 1;
                    break;
                case "staff":
                    user.Role = "Staff";
                    user.PermissionLevel = 2;
                    break;
                case "admin":
                    user.Role = "Admin";
                    user.PermissionLevel = 3;
                    break;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật vai trò thành công." });
        }

        public async Task<IActionResult> NewsManagement(int? editId = null)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            var newsList = await _context.News
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            if (editId.HasValue)
            {
                var editingNews = await _context.News.FindAsync(editId.Value);
                if (editingNews != null)
                {
                    ViewBag.EditingNews = editingNews;
                }
            }

            return View(newsList);
        }

        [HttpPost]
        public async Task<IActionResult> EditNews(News updatedNews)
        {
            var news = await _context.News.FindAsync(updatedNews.NewsId);
            if (news == null) return NotFound();

            news.Title = updatedNews.Title;
            news.Url = updatedNews.Url;
            news.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật tin tức thành công!";
            return RedirectToAction("NewsManagement");
        }

        public IActionResult CreateNews()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return RedirectToAction("Index", "Login");
            }

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> CreateNews(string Title, string Url)
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(username) || role?.ToLower() != "admin")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." });
            }

            // Check input data
            if (string.IsNullOrEmpty(Title) || string.IsNullOrEmpty(Url))
            {
                return Json(new { success = false, message = "Tiêu đề và URL không được để trống." });
            }

            try
            {
                // Create new news
                var newNews = new News
                {
                    Title = Title,
                    Url = Url,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Type = "news"
                };

                _context.News.Add(newNews);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm tin tức: " + ex.Message });
            }
        }

    }
}
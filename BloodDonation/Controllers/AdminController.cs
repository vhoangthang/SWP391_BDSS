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

            // Lấy thống kê tổng quan
            var totalUsers = await _context.Accounts.CountAsync();
            var totalBloodRequests = await _context.BloodRequests.CountAsync();
            var totalBloodInventory = await _context.BloodInventories.SumAsync(bi => bi.Quantity);

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalBloodRequests = totalBloodRequests;
            ViewBag.TotalBloodInventory = totalBloodInventory;

            return View();
        }

        // Quản lý người dùng
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

            return View(users);
        }

        // Quản lý yêu cầu nhận máu
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

            return View(bloodRequests);
        }

        // Quản lý kho máu
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

        // Cập nhật trạng thái yêu cầu nhận máu
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

        // Cập nhật số lượng kho máu
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
                // Giả sử chỉ có 1 ngân hàng máu (BloodBankID = 1)
                const int bloodBankId = 1;

                var bloodInventory = await _context.BloodInventories.FirstOrDefaultAsync(i => i.BloodTypeID == bloodTypeId && i.BloodBankID == bloodBankId);
                
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
    }
} 
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BloodDonation.Data;
using BloodDonation.Models;
using System;
using System.Linq;
using System.Text.Json;


namespace BloodDonation.Controllers
{
    public class StaffController : Controller
    {
        private readonly AppDbContext _context;

        public StaffController(AppDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách các yêu cầu đăng ký hiến máu
        public IActionResult DonationList()
        {
            var appointments = _context.DonationAppointments
           .Include(a => a.Donor)
           .Include(a => a.MedicalCenter)
           .Include(a => a.BloodType)
           .OrderByDescending(a => a.AppointmentDate)
           .ToList();

            return View(appointments);
        }

        public IActionResult DonorRequestDetails(int id)
        {
            // Lấy appointment theo ID (AppointmentID)
            var appointment = _context.DonationAppointments
                .Include(x => x.Donor)
                .Include(x => x.MedicalCenter)
                .Include(x => x.BloodType)
                .FirstOrDefault(x => x.AppointmentID == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Parse JSON từ chuỗi HealthSurvey nếu có
            Dictionary<string, bool> healthSurvey = new();

            if (!string.IsNullOrEmpty(appointment.HealthSurvey))
            {
                try
                {
                    healthSurvey = JsonSerializer.Deserialize<Dictionary<string, bool>>(appointment.HealthSurvey);
                }
                catch (Exception ex)
                {
                    // Log nếu JSON lỗi (tùy bạn xử lý)
                    Console.WriteLine("Lỗi parse JSON: " + ex.Message);
                }
            }

            ViewData["HealthSurvey"] = healthSurvey;
            ViewData["Menu"] = "donor";

            return View("DonorRequestDetails", appointment);
        }

        public IActionResult BloodInventory()
        {
            var inventories = _context.BloodInventories
                .Include(b => b.BloodType)
                .ToList();

            // Tính tổng số lượng máu
            decimal totalQuantity = inventories.Sum(b => b.Quantity);

            // Gửi vào View qua ViewBag hoặc ViewModel
            ViewBag.TotalQuantity = totalQuantity;

            return View(inventories);
        }

        //Xác nhận yêu cầu hiến máu và cập nhật kho máu
        [HttpPost]
        public IActionResult ConfirmDonation(int AppointmentID, bool IsEligible, decimal QuantityDonated, string action)
        {
            try
            {
                string normalizedAction = action?.ToLowerInvariant();
                string sql = null;

                if (normalizedAction == "completed")
                {
                    // Cập nhật trạng thái Completed và gán số lượng máu hiến (QuantityDonated)
                    sql = @"
                UPDATE DonationAppointment
                SET Status = 'Completed',
                    QuantityDonated = {1}
                WHERE AppointmentID = {0} AND BloodTypeID IS NOT NULL AND Status != 'Completed'";
                }
                else if (normalizedAction == "confirm" && IsEligible)
                {
                    sql = @"
                UPDATE DonationAppointment
                SET Status = 'Confirmed',
                    AppointmentDate = GETDATE()
                WHERE AppointmentID = {0}";
                }
                else if (normalizedAction == "reject")
                {
                    sql = "UPDATE DonationAppointment SET Status = 'Rejected' WHERE AppointmentID = {0}";
                }

                if (!string.IsNullOrEmpty(sql))
                {
                    int rows = _context.Database.ExecuteSqlRaw(sql, AppointmentID, QuantityDonated);
                    TempData["Message"] = rows > 0
                        ? "✅ Trạng thái đã được cập nhật."
                        : "⚠️ Không có bản ghi nào được cập nhật. Có thể trạng thái đã là Completed.";
                }
                else
                {
                    TempData["Error"] = "❌ Hành động không hợp lệ.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi xảy ra: {ex.Message}";
            }

            return RedirectToAction("DonationList");
        }


        public IActionResult BloodRequestList()
        {
            var approvedRequests = _context.BloodRequests
                .Include(r => r.MedicalCenter)
                .Include(r => r.BloodType)
                //.Where(r => r.Status == "Approved")
                .ToList();

            return View(approvedRequests);
        }

        public IActionResult ProcessBloodRequest(int id)
        {
            var request = _context.BloodRequests
        .Include(r => r.MedicalCenter)
        .Include(r => r.BloodType)
        .FirstOrDefault(r => r.BloodRequestID == id);

            if (request == null || request.Status == "Completed" || request.Status == "Rejected")
                return NotFound(); // không cho xử lý yêu cầu đã bị từ chối hoặc đã hoàn thành

            return View(request);
        }

        [HttpPost]
        public IActionResult CompleteBloodRequest(int id, string note)
        {
            var request = _context.BloodRequests
                .Include(r => r.BloodType)
                .FirstOrDefault(r => r.BloodRequestID == id);

            if (request == null)
            {
                TempData["Error"] = "❌ Yêu cầu không tồn tại.";
                return RedirectToAction("BloodRequestList");
            }

            // Giả sử chỉ có 1 ngân hàng máu (BloodBankID = 1)
            int bloodBankId = 1;

            var inventory = _context.BloodInventories
                .FirstOrDefault(b => b.BloodTypeID == request.BloodTypeID && b.BloodBankID == bloodBankId);

            if (inventory != null && inventory.Quantity >= request.Quantity)
            {
                // Trừ lượng máu
                inventory.Quantity -= request.Quantity;
                inventory.LastUpdated = DateTime.Now;

                request.Status = "Completed";
                TempData["Message"] = "✅ Yêu cầu đã được xử lý và cập nhật kho máu.";
            }
            else
            {
                request.Status = "Pending";
                TempData["Info"] = "⚠️ Kho máu không đủ. Yêu cầu chuyển sang trạng thái chờ.";
            }

            //request.Note = note;
            _context.SaveChanges();

            return RedirectToAction("BloodRequestList");
        }

        public IActionResult AppointmentDetail(int id)
        {
            var appointment = _context.DonationAppointments
                .Include(a => a.Donor)
                .Include(a => a.BloodType)
                .FirstOrDefault(a => a.AppointmentID == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // Flag xác định có được phép xử lý hay không
            ViewBag.AllowEdit = appointment.Status == "Pending" || appointment.Status == "Confirmed";

            // Nếu bạn cần gửi survey nữa
            var surveyDict = JsonSerializer.Deserialize<Dictionary<string, bool>>(appointment.HealthSurvey ?? "{}");
            ViewData["HealthSurvey"] = surveyDict;

            return View(appointment);
        }
    }
}

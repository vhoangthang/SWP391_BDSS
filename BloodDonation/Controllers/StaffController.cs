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

        // Helper method to check staff login
        private bool IsStaffLoggedIn()
        {
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");
            return !string.IsNullOrEmpty(username) && role?.ToLower() == "staff";
        }

        // Hiển thị danh sách các yêu cầu đăng ký hiến máu
        public IActionResult DonationList()
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
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
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
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
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
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
        public IActionResult ConfirmDonation(int AppointmentID, string IsEligible, decimal QuantityDonated, string action)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");

            try
            {
                var appointment = _context.DonationAppointments.FirstOrDefault(a => a.AppointmentID == AppointmentID);

                if (appointment == null)
                {
                    TempData["Error"] = "❌ Không tìm thấy lịch hẹn.";
                    return RedirectToAction("DonationList");
                }

                string normalizedAction = action?.ToLowerInvariant();
                bool isEligible = IsEligible == "true";

                switch (normalizedAction)
                {
                    case "completed":
                        if (appointment.Status == "Completed")
                        {
                            TempData["Error"] = "⚠️ Lịch hẹn đã hoàn thành trước đó.";
                            return RedirectToAction("DonationList");
                        }

                        if (appointment.BloodTypeID <= 0)
                        {
                            TempData["Error"] = "⚠️ Thiếu thông tin nhóm máu.";
                            return RedirectToAction("DonationList");
                        }

                        if (QuantityDonated <= 0)
                        {
                            TempData["Error"] = "⚠️ Số lượng máu hiến phải lớn hơn 0.";
                            return RedirectToAction("DonationList");
                        }

                        appointment.Status = "Completed";
                        appointment.QuantityDonated = QuantityDonated;

                        // ✅ Cập nhật hoặc tạo mới kho máu
                        var inventory = _context.BloodInventories.FirstOrDefault(b => b.BloodTypeID == appointment.BloodTypeID);
                        if (inventory != null)
                        {
                            inventory.Quantity += QuantityDonated;
                            inventory.LastUpdated = DateTime.Now;
                        }
                        else
                        {
                            var newInventory = new BloodInventory
                            {
                                BloodTypeID = appointment.BloodTypeID,
                                BloodBankID = 1, // TODO: Lấy đúng BloodBankID nếu có logic, tạm mặc định là 1
                                Quantity = QuantityDonated,
                                LastUpdated = DateTime.Now
                            };
                            _context.BloodInventories.Add(newInventory);
                        }

                        // *** TẠO CERTIFICATE ***
                        var existingCertificate = _context.DonationCertificates
                            .FirstOrDefault(c => c.AppointmentID == appointment.AppointmentID);
                        if (existingCertificate == null)
                        {
                            var donor = _context.Donors.FirstOrDefault(d => d.DonorID == appointment.DonorID);
                            var bloodType = _context.BloodTypes.FirstOrDefault(b => b.BloodTypeID == appointment.BloodTypeID);
                            var medicalCenter = _context.MedicalCenters.FirstOrDefault(m => m.MedicalCenterID == appointment.MedicalCenterID);

                            string details = $"Chứng chỉ hiến máu cho {(donor?.Name ?? "Người hiến máu")}, nhóm máu {bloodType?.Type ?? "?"}, hiến {appointment.QuantityDonated}CC tại {medicalCenter?.Name ?? "cơ sở y tế"} ngày {appointment.AppointmentDate:dd/MM/yyyy}.";

                            var certificate = new DonationCertificate
                            {
                                AppointmentID = appointment.AppointmentID,
                                IssueDate = DateTime.Now,
                                CertificateDetails = details
                            };
                            _context.DonationCertificates.Add(certificate);
                        }
                        break;

                    case "confirm":
                        if (isEligible)
                        {
                            appointment.Status = "Confirmed";
                            appointment.AppointmentDate = DateTime.Now;
                        }
                        else
                        {
                            TempData["Error"] = "❌ Người hiến máu không đủ điều kiện.";
                            return RedirectToAction("DonationList");
                        }
                        break;

                    case "reject":
                        appointment.Status = "Rejected";
                        break;

                    case "approve":
                        appointment.Status = "Approved";
                        break;

                    default:
                        TempData["Error"] = "❌ Hành động không hợp lệ.";
                        return RedirectToAction("DonationList");
                }

                // ✅ Lưu thay đổi
                _context.SaveChanges();
                TempData["Message"] = "✅ Trạng thái đã được cập nhật.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Lỗi xảy ra: {ex.Message}";
                Console.WriteLine($"[ERROR] ConfirmDonation: {ex.Message}");
            }

            return RedirectToAction("DonationList");
        }




        public IActionResult BloodRequestList()
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
            var approvedRequests = _context.BloodRequests
                .Include(r => r.MedicalCenter)
                .Include(r => r.BloodType)
                //.Where(r => r.Status == "Approved")
                .ToList();

            return View(approvedRequests);
        }

        public IActionResult ProcessBloodRequest(int id)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
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
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
            var request = _context.BloodRequests
                .Include(r => r.BloodType)
                .FirstOrDefault(r => r.BloodRequestID == id);

            if (request == null)
            {
                TempData["Error"] = "❌ Yêu cầu không tồn tại.";
                return RedirectToAction("BloodRequestList");
            }

            // ❌ Nếu trạng thái không phải Approved thì không xử lý
            if (!string.Equals(request.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "⚠️ Chỉ xử lý các yêu cầu có trạng thái 'Approved'.";
                return RedirectToAction("BloodRequestList");
            }

            int bloodBankId = 1; // Mặc định
            var inventory = _context.BloodInventories
                .FirstOrDefault(b => b.BloodTypeID == request.BloodTypeID && b.BloodBankID == bloodBankId);

            if (inventory != null && inventory.Quantity >= request.Quantity)
            {
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

            _context.SaveChanges();
            return RedirectToAction("BloodRequestList");
        }

        public IActionResult AppointmentDetail(int id)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
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

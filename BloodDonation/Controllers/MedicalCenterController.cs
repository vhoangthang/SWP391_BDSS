﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BloodDonation.Data;
using BloodDonation.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BloodDonation.Controllers
{
    public class MedicalCenterController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MedicalCenterController> _logger;

        public MedicalCenterController(AppDbContext context, ILogger<MedicalCenterController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var medicalCenterId = HttpContext.Session.GetInt32("MedicalCenterID");
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            _logger.LogInformation($"Session Info - MedicalCenterID: {medicalCenterId}, Username: {username}, Role: {role}");

            if (!medicalCenterId.HasValue)
            {
                _logger.LogWarning("MedicalCenterID not found in session, redirecting to login");
                return RedirectToAction("Index", "Login");
            }

            // Đồng bộ logic badge: lấy tổng số thông báo chưa đọc của tất cả account thuộc Medical Center
            var medicalCenterAccount = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "medicalcenter");
            int unreadCount = 0;
            if (medicalCenterAccount != null)
            {
                var accountIds = _context.Accounts
                    .Where(a => a.MedicalCenterID == medicalCenterAccount.MedicalCenterID && a.Role.ToLower() == "medicalcenter")
                    .Select(a => a.AccountID)
                    .ToList();
                unreadCount = _context.Notifications.Count(n => accountIds.Contains((int)n.AccountID) && !n.IsRead);
            }
            ViewBag.UnreadNotificationCount = unreadCount;

            // Debug: Log all blood types
            var bloodTypes = _context.BloodTypes.ToList();
            foreach (var bt in bloodTypes)
            {
                _logger.LogInformation($"BloodType: ID={bt.BloodTypeID}, Type={bt.Type}");
            }

            var model = new BloodRequestViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BloodRequestViewModel model)
        {
            var medicalCenterId = HttpContext.Session.GetInt32("MedicalCenterID");
            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            _logger.LogInformation($"Create Action - MedicalCenterID: {medicalCenterId}, Username: {username}, Role: {role}");

            if (!medicalCenterId.HasValue)
            {
                _logger.LogWarning("MedicalCenterID not found in session during Create action");
                return RedirectToAction("Index", "Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var bloodType = await _context.BloodTypes.FirstOrDefaultAsync(bt => bt.Type == model.BloodType);
                    if (bloodType == null)
                    {
                        _logger.LogError($"BloodType not found for: {model.BloodType}");
                        ModelState.AddModelError("BloodType", "Nhóm máu không hợp lệ.");
                        return View("Index", model);
                    }

                    _logger.LogInformation($"Found BloodType: ID={bloodType.BloodTypeID}, Type={bloodType.Type}");

                    var bloodRequest = new BloodRequest
                    {
                        MedicalCenterID = medicalCenterId.Value,
                        BloodTypeID = bloodType.BloodTypeID,
                        Reason = model.Reason,
                        RequestDate = DateTime.Now,
                        Quantity = model.Quantity,
                        IsEmergency = model.ReceptionType == "emergencyReception",
                        IsCompatible = model.Compatibility == "Tương hợp",
                        Status = BloodRequestStatus.Pending
                    };

                    _logger.LogInformation($"Creating BloodRequest: MedicalCenterID={bloodRequest.MedicalCenterID}, BloodTypeID={bloodRequest.BloodTypeID}, Quantity={bloodRequest.Quantity}");

                    _context.Add(bloodRequest);

                    // Log the SQL that will be executed
                    _logger.LogInformation("About to save changes to database...");

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"BloodRequest created successfully with ID: {bloodRequest.BloodRequestID}");

                    // Add success message with status
                    TempData["SuccessMessage"] = $"Đăng ký nhận máu thành công! Mã yêu cầu: {bloodRequest.BloodRequestID} - Trạng thái: {bloodRequest.Status}";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error creating BloodRequest: {ex.Message}");
                    _logger.LogError($"Stack trace: {ex.StackTrace}");

                    // Log inner exception if exists
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }

                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng thử lại.");
                    return View("Index", model);
                }
            }
            else
            {
                _logger.LogWarning($"ModelState is invalid: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
            }

            return View("Index", model);
        }

        // Display list of blood requests for the current medical center
        public IActionResult BloodRequestList()
        {
            var medicalCenterId = HttpContext.Session.GetInt32("MedicalCenterID");
            var username = HttpContext.Session.GetString("Username");
            if (!medicalCenterId.HasValue)
            {
                _logger.LogWarning("MedicalCenterID not found in session, redirecting to login");
                return RedirectToAction("Index", "Login");
            }

            // Tính badge
            var medicalCenterAccount = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "medicalcenter");
            int unreadCount = 0;
            if (medicalCenterAccount != null)
            {
                var accountIds = _context.Accounts
                    .Where(a => a.MedicalCenterID == medicalCenterAccount.MedicalCenterID && a.Role.ToLower() == "medicalcenter")
                    .Select(a => a.AccountID)
                    .ToList();
                unreadCount = _context.Notifications.Count(n => accountIds.Contains((int)n.AccountID) && !n.IsRead);
            }
            ViewBag.UnreadNotificationCount = unreadCount;

            var requests = _context.BloodRequests
                .Include(r => r.BloodType)
                .Where(r => r.MedicalCenterID == medicalCenterId.Value)
                .OrderByDescending(r => r.RequestDate)
                .ToList();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelBloodRequest(int id)
        {
            var medicalCenterId = HttpContext.Session.GetInt32("MedicalCenterID");
            if (!medicalCenterId.HasValue)
            {
                _logger.LogWarning("MedicalCenterID not found in session, redirecting to login");
                return RedirectToAction("Index", "Login");
            }
            var request = _context.BloodRequests.FirstOrDefault(r => r.BloodRequestID == id && r.MedicalCenterID == medicalCenterId.Value);
            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu hoặc bạn không có quyền huỷ.";
                return RedirectToAction("BloodRequestList");
            }
            if (request.Status == "Pending")
            {
                request.Status = "Canceled";
                _context.SaveChanges();
                TempData["Success"] = "Đã huỷ yêu cầu thành công.";
            }
            else
            {
                TempData["Error"] = "Chỉ có thể huỷ yêu cầu khi trạng thái là Pending.";
            }
            return RedirectToAction("BloodRequestList");
        }

        public IActionResult MedicalCenterNotifications()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var medicalCenterAccount = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "medicalcenter");
            if (medicalCenterAccount == null)
                return RedirectToAction("Index", "Login");

            // Get the MedicalCenterID of the current account
            var medicalCenterId = medicalCenterAccount.MedicalCenterID;

            // Get all MedicalCenter AccountIDs of the same facility
            var accountIds = _context.Accounts
                .Where(a => a.MedicalCenterID == medicalCenterId && a.Role.ToLower() == "medicalcenter")
                .Select(a => a.AccountID)
                .ToList();

            // Get notifications of all these accounts
            var notifications = _context.Notifications
                .Include(n => n.Donor)
                .Where(n => accountIds.Contains((int)n.AccountID))
                .OrderByDescending(n => n.SentAt)
                .ToList();

            // Number of unreads
            int unreadCount = 0;
            unreadCount = _context.Notifications.Count(n => n.AccountID == medicalCenterAccount.AccountID && !n.IsRead);
            unreadCount = _context.Notifications.Count(n => accountIds.Contains((int)n.AccountID) && !n.IsRead);
            ViewBag.UnreadNotificationCount = unreadCount;

            return View("MedicalCenterNotifications", notifications);
        }

        public IActionResult NotificationDetail(int notificationId)
        {
            if (notificationId <= 0)
            {
                TempData["Error"] = "Không tìm thấy thông báo phù hợp.";
                return RedirectToAction("MedicalCenterNotifications");
            }
            var notification = _context.Notifications
                .Include(n => n.Donor)
                    .ThenInclude(d => d.Account)
                .Include(n => n.BloodRequest)
                .FirstOrDefault(n => n.NotificationID == notificationId);
            if (notification == null)
            {
                TempData["Error"] = "Không tìm thấy thông báo phù hợp.";
                return RedirectToAction("MedicalCenterNotifications");
            }

            DonationAppointment appointment = null;
            if (notification.BloodRequestID.HasValue && notification.BloodRequest != null)
            {
                // Get the most recent appointment of the donor with status Confirmed or Completed
                appointment = _context.DonationAppointments
                    .Include(a => a.Donor)
                    .Include(a => a.MedicalCenter)
                    .Include(a => a.BloodType)
                    .Where(a => a.DonorID == notification.DonorID && (a.Status == "Confirmed" || a.Status == "Completed"))
                    .OrderByDescending(a => a.AppointmentDate)
                    .FirstOrDefault();
            }
            else
            {
                // If no BloodRequestID, get the most recent appointment of the donor
                appointment = _context.DonationAppointments
                    .Include(a => a.Donor)
                    .Include(a => a.MedicalCenter)
                    .Include(a => a.BloodType)
                    .Where(a => a.DonorID == notification.DonorID)
                    .OrderByDescending(a => a.AppointmentDate)
                    .FirstOrDefault();
            }
            ViewBag.Appointment = appointment;
            return View("NotificationDetail", notification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmAppointmentCompletion(int appointmentId)
        {
            var appointment = _context.DonationAppointments
                .Include(a => a.Donor)
                .Include(a => a.BloodType)
                .Include(a => a.MedicalCenter)
                .FirstOrDefault(a => a.AppointmentID == appointmentId);
            if (appointment == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hiến máu.";
                return RedirectToAction("MedicalCenterNotifications");
            }

            // Find related notification if any
            var notification = _context.Notifications.FirstOrDefault(n => n.DonorID == appointment.DonorID && n.BloodRequestID != null);

            // Find related BloodRequest
            BloodRequest bloodRequest = null;
            if (notification != null && notification.BloodRequestID.HasValue)
            {
                bloodRequest = _context.BloodRequests.FirstOrDefault(br => br.BloodRequestID == notification.BloodRequestID.Value);
            }
            if (bloodRequest == null)
            {
                bloodRequest = _context.BloodRequests.FirstOrDefault(br => br.BloodTypeID == appointment.BloodTypeID && br.MedicalCenterID == appointment.MedicalCenterID);
            }

            // Update status and information for both tables
            if (bloodRequest != null)
            {
                bloodRequest.Status = "Completed";
                bloodRequest.BloodGiven = appointment.BloodType?.Type;
                // Update QuantityDonated for the appointment
                appointment.Status = "Completed";
                appointment.QuantityDonated = bloodRequest.Quantity;
                appointment.AppointmentDate = DateTime.Now; // Update completion date
            }
            else
            {
                // If bloodRequest is not found, still allow completing the appointment
                appointment.Status = "Completed";
                appointment.AppointmentDate = DateTime.Now; // Update completion date
            }

            // Save to DonorBloodRequest table if bloodRequest and donor exist
            if (bloodRequest != null && appointment.Donor != null)
            {
                var donorBloodRequest = new DonorBloodRequest
                {
                    BloodRequestID = bloodRequest.BloodRequestID,
                    DonorID = appointment.Donor.DonorID,
                    DonationDate = DateTime.Now,
                    QuantityDonated = bloodRequest.Quantity
                };
                _context.DonorBloodRequests.Add(donorBloodRequest);
            }

            // If donor is available, change to not available
            if (appointment.Donor != null && appointment.Donor.IsAvailable == true)
            {
                appointment.Donor.IsAvailable = false;
                _context.SaveChanges();
            }

            // Create certificate if it doesn't exist
            var existingCertificate = _context.DonationCertificates.FirstOrDefault(c => c.AppointmentID == appointment.AppointmentID);
            if (existingCertificate == null)
            {
                decimal quantity = bloodRequest?.Quantity ?? appointment.QuantityDonated;
                var issueDate = DateTime.Now;
                var certificate = new DonationCertificate
                {
                    AppointmentID = appointment.AppointmentID,
                    IssueDate = issueDate,
                    CertificateDetails = $"Chứng chỉ hiến máu cho {(appointment.Donor?.Name ?? "Người hiến máu")}, nhóm máu {appointment.BloodType?.Type ?? "?"}, hiến {quantity}CC tại {appointment.MedicalCenter?.Name ?? "cơ sở y tế"} ngày {issueDate:dd/MM/yyyy}."
                };
                _context.DonationCertificates.Add(certificate);
            }
            _context.SaveChanges();
            TempData["Message"] = "Đã xác nhận hoàn thành và cấp chứng chỉ cho người hiến máu.";
            return RedirectToAction("NotificationDetail", new { notificationId = appointmentId, appointmentId = appointment.AppointmentID });
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var account = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "medicalcenter");
            if (account == null)
                return Json(new { success = false, message = "Không tìm thấy tài khoản" });

            var notification = _context.Notifications.FirstOrDefault(n => n.NotificationID == id && n.AccountID == account.AccountID);
            if (notification == null)
                return Json(new { success = false, message = "Không tìm thấy thông báo" });

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteNotification(int notificationId)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var account = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "medicalcenter");
            if (account == null)
                return Json(new { success = false, message = "Không tìm thấy tài khoản" });

            var notification = _context.Notifications.FirstOrDefault(n => n.NotificationID == notificationId && n.AccountID == account.AccountID);
            if (notification == null)
                return Json(new { success = false, message = "Không tìm thấy thông báo" });

            _context.Notifications.Remove(notification);
            _context.SaveChanges();
            return Json(new { success = true });
        }
    }
}
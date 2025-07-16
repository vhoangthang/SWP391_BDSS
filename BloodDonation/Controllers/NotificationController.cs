using Microsoft.AspNetCore.Mvc;
using BloodDonation.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using BloodDonation.Models;

namespace BloodDonation.Controllers
{
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;
        public NotificationController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var donor = _context.Donors.Include(d => d.Account).FirstOrDefault(d => d.Account.Username == username);
            if (donor == null)
                return RedirectToAction("Index", "Login");

            // Chỉ lấy thông báo có AccountID đúng với tài khoản donor
            var notifications = _context.Notifications
                .Where(n => n.AccountID == donor.AccountID)
                .OrderByDescending(n => n.SentAt)
                .ToList();
            return View(notifications);
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var donor = _context.Donors.Include(d => d.Notifications).Include(d => d.Account).FirstOrDefault(d => d.Account.Username == username);
            if (donor == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng" });

            var notification = donor.Notifications.FirstOrDefault(n => n.NotificationID == id);
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
        public IActionResult MarkAsReadStaff(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var staff = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "staff");
            if (staff == null)
                return Json(new { success = false, message = "Không tìm thấy tài khoản staff" });

            var notification = _context.Notifications.FirstOrDefault(n => n.NotificationID == id && n.AccountID == staff.AccountID);
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
        public IActionResult SendInviteNotification(int donorId, int bloodRequestId)
        {
            var donor = _context.Donors.Include(d => d.Account).FirstOrDefault(d => d.DonorID == donorId);
            if (donor == null)
                return NotFound();

            // Lấy thông tin blood request và medical center
            var bloodRequest = _context.BloodRequests.Include(br => br.MedicalCenter).FirstOrDefault(br => br.BloodRequestID == bloodRequestId);
            string medicalCenterInfo = "";
            if (bloodRequest != null && bloodRequest.MedicalCenter != null)
            {
                medicalCenterInfo = $"\nĐịa điểm: {bloodRequest.MedicalCenter.Name} ({bloodRequest.MedicalCenter.Location})";
            }

            var notification = new Notification
            {
                DonorID = donorId,
                Message = "Bạn được mời tham gia hiến máu cho một trường hợp cần thiết. Vui lòng xác nhận nếu bạn đồng ý." + medicalCenterInfo,
                SentAt = DateTime.Now,
                IsRead = false,
                Type = "Invite",
                IsConfirmed = false,
                AccountID = donor.AccountID,
                BloodRequestID = bloodRequestId
            };
            _context.Notifications.Add(notification);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đã gửi thông báo mời hiến máu thành công!";

            return RedirectToAction("NearestDonorsWithin20km", "Staff", new { bloodRequestId = bloodRequestId });
        }

        [HttpPost]
        public IActionResult ConfirmDonation(int notificationId)
        {
            var notification = _context.Notifications
                .Include(n => n.Donor)
                .ThenInclude(d => d.BloodType)
                .FirstOrDefault(n => n.NotificationID == notificationId);
            if (notification == null)
                return NotFound();

            notification.IsConfirmed = true;
            _context.SaveChanges();

            // Lấy thông tin BloodRequest liên quan
            var bloodRequest = _context.BloodRequests
                .Include(br => br.MedicalCenter)
                .FirstOrDefault(br => br.BloodRequestID == notification.BloodRequestID);

            if (bloodRequest != null)
            {
                // Cập nhật MedicalCenterID của đơn hiến máu gần nhất (Confirmed) thành MedicalCenterID của BloodRequest
                var appointment = _context.DonationAppointments
                    .Where(a => a.DonorID == notification.DonorID && a.Status == "Confirmed")
                    .OrderByDescending(a => a.AppointmentDate)
                    .FirstOrDefault();
                if (appointment != null)
                {
                    appointment.MedicalCenterID = bloodRequest.MedicalCenterID;
                    _context.SaveChanges();
                }
                // Gửi thông báo cho tất cả staff (không lọc theo MedicalCenterID)
                var staffAccounts = _context.Accounts.Where(a => a.Role.ToLower() == "staff").ToList();
                foreach (var staffAccount in staffAccounts)
                {
                    var staffNotification = new Notification
                    {
                        DonorID = notification.DonorID,
                        Message = $"Donor {notification.Donor.Name} (ID: {notification.Donor.DonorID}, Nhóm máu: {notification.Donor.BloodType?.Type}) đã xác nhận đi hiến máu.",
                        SentAt = DateTime.Now,
                        IsRead = false,
                        Type = "DonorConfirmed",
                        IsConfirmed = true,
                        AccountID = staffAccount.AccountID,
                        BloodRequestID = bloodRequest.BloodRequestID
                    };
                    _context.Notifications.Add(staffNotification);
                }

                // Medical center notification giữ nguyên
                var medicalCenterAccount = _context.Accounts.FirstOrDefault(a => a.Role.ToLower() == "medicalcenter" && a.MedicalCenterID == bloodRequest.MedicalCenterID);
                if (medicalCenterAccount != null)
                {
                    var medicalCenterNotification = new Notification
                    {
                        DonorID = notification.DonorID,
                        Message = $"Donor {notification.Donor.Name} (ID: {notification.Donor.DonorID}, Nhóm máu: {notification.Donor.BloodType?.Type}) đã xác nhận đi hiến máu.",
                        SentAt = DateTime.Now,
                        IsRead = false,
                        Type = "DonorConfirmed",
                        IsConfirmed = true,
                        AccountID = medicalCenterAccount.AccountID,
                        BloodRequestID = bloodRequest.BloodRequestID
                    };
                    _context.Notifications.Add(medicalCenterNotification);
                }
                _context.SaveChanges();
            }
            else
            {
                // Nếu không có BloodRequestID, gửi cho tất cả staff
                var allStaff = _context.Accounts.Where(a => a.Role.ToLower() == "staff").ToList();
                foreach (var staffAccount in allStaff)
                {
                    var staffNotification = new Notification
                    {
                        DonorID = notification.DonorID,
                        Message = $"Donor {notification.Donor.Name} (ID: {notification.Donor.DonorID}, Nhóm máu: {notification.Donor.BloodType?.Type}) đã xác nhận đi hiến máu.",
                        SentAt = DateTime.Now,
                        IsRead = false,
                        Type = "DonorConfirmed",
                        IsConfirmed = true,
                        AccountID = staffAccount.AccountID
                    };
                    _context.Notifications.Add(staffNotification);
                }
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RejectDonation(int notificationId)
        {
            var notification = _context.Notifications
                .Include(n => n.Donor)
                .ThenInclude(d => d.BloodType)
                .FirstOrDefault(n => n.NotificationID == notificationId);
            if (notification == null)
                return NotFound();

            notification.IsConfirmed = false; // Đánh dấu đã từ chối
            _context.SaveChanges();

            // Lấy thông tin BloodRequest liên quan
            var bloodRequest = _context.BloodRequests
                .Include(br => br.MedicalCenter)
                .FirstOrDefault(br => br.BloodRequestID == notification.BloodRequestID);

            if (bloodRequest != null)
            {
                // Gửi thông báo cho tất cả staff liên quan
                var staffAccounts = _context.Accounts.Where(a => a.Role.ToLower() == "staff").ToList();
                foreach (var staffAccount in staffAccounts)
                {
                    var staffNotification = new Notification
                    {
                        DonorID = notification.DonorID,
                        Message = $"Donor {notification.Donor.Name} (ID: {notification.Donor.DonorID}, Nhóm máu: {notification.Donor.BloodType?.Type}) đã từ chối lời mời hiến máu.",
                        SentAt = DateTime.Now,
                        IsRead = false,
                        Type = "DonorRejected",
                        IsConfirmed = false,
                        AccountID = staffAccount.AccountID,
                        BloodRequestID = bloodRequest.BloodRequestID
                    };
                    _context.Notifications.Add(staffNotification);
                }
                _context.SaveChanges();
            }
            else
            {
                // Nếu không có BloodRequestID, gửi cho tất cả staff
                var allStaff = _context.Accounts.Where(a => a.Role.ToLower() == "staff").ToList();
                foreach (var staffAccount in allStaff)
                {
                    var staffNotification = new Notification
                    {
                        DonorID = notification.DonorID,
                        Message = $"Donor {notification.Donor.Name} (ID: {notification.Donor.DonorID}, Nhóm máu: {notification.Donor.BloodType?.Type}) đã từ chối lời mời hiến máu.",
                        SentAt = DateTime.Now,
                        IsRead = false,
                        Type = "DonorRejected",
                        IsConfirmed = false,
                        AccountID = staffAccount.AccountID
                    };
                    _context.Notifications.Add(staffNotification);
                }
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteNotification(int notificationId)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var donor = _context.Donors.Include(d => d.Account).FirstOrDefault(d => d.Account.Username == username);
            if (donor == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng" });

            var notification = _context.Notifications.FirstOrDefault(n => n.NotificationID == notificationId && n.AccountID == donor.AccountID);
            if (notification == null)
                return Json(new { success = false, message = "Không tìm thấy thông báo" });

            _context.Notifications.Remove(notification);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult DeleteNotificationStaff(int notificationId)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Chưa đăng nhập" });

            var staff = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "staff");
            if (staff == null)
                return Json(new { success = false, message = "Không tìm thấy tài khoản staff" });

            var notification = _context.Notifications.FirstOrDefault(n => n.NotificationID == notificationId && n.AccountID == staff.AccountID);
            if (notification == null)
                return Json(new { success = false, message = "Không tìm thấy thông báo" });

            _context.Notifications.Remove(notification);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        public IActionResult StaffNotifications()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var staffAccount = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "staff");
            if (staffAccount == null)
                return RedirectToAction("Index", "Login");

            // Thêm logic badge
            int unreadCount = 0;
            unreadCount = _context.Notifications.Count(n => n.AccountID == staffAccount.AccountID && !n.IsRead);
            ViewBag.UnreadNotificationCount = unreadCount;

            var notifications = _context.Notifications
                .Include(n => n.Donor)
                .Where(n => n.AccountID == staffAccount.AccountID)
                .OrderByDescending(n => n.SentAt)
                .ToList();

            return View("StaffNotifications", notifications);
        }
    }
}

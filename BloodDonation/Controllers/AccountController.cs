using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Linq;

namespace BloodDonation.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private static string sentCode = "";
        private static string resetCode = "";
        private static string resetEmail = "";

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.VerificationCode != sentCode)
            {
                ModelState.AddModelError("", "Mã xác minh không đúng.");
                return View(model);
            }

            // TODO: Lưu thông tin tài khoản vào DB
            TempData["Success"] = "Đăng ký thành công!";
            return RedirectToAction("Index", "Login");
        }

        [HttpPost]
        public async Task<IActionResult> SendVerificationCode([FromBody] EmailRequest request)
        {
            string email = request.Email;
            sentCode = new Random().Next(100000, 999999).ToString();

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("ngvhuy1612@gmail.com", "avobgwpxoctsanga"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("ngvhuy1612@gmail.com"),
                Subject = "Mã xác minh đăng ký",
                Body = $"Mã xác minh của bạn là: {sentCode}",
                IsBodyHtml = false,
            };

            mailMessage.To.Add(email);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi email: " + ex.Message);
                return Json(new { success = false, message = "Gửi mã thất bại: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            resetEmail = email;
            resetCode = new Random().Next(100000, 999999).ToString();

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("ngvhuy1612@gmail.com", "avobgwpxoctsanga"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("ngvhuy1612@gmail.com"),
                Subject = "Mã xác minh đặt lại mật khẩu",
                Body = $"Mã xác minh của bạn là: {resetCode}",
                IsBodyHtml = false,
            };
            mailMessage.To.Add(email);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                TempData["Email"] = email;
                return RedirectToAction("VerifyResetCode");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Không thể gửi mã: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult VerifyResetCode()
        {
            return View();
        }

        [HttpPost]
        public IActionResult VerifyResetCode(string code)
        {
            if (code == resetCode)
            {
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Mã xác minh không đúng.";
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string newPassword)
        {
            var account = _context.Accounts.FirstOrDefault(a => a.Email == resetEmail);

            if (account == null)
            {
                ViewBag.Error = "Tài khoản không tồn tại.";
                return View();
            }

            account.Password = newPassword; // Mã hóa nếu cần
            _context.SaveChanges();

            return RedirectToAction("Login");
        }
    }
}
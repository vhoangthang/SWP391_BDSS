using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace BloodDonation.Controllers
{
    public class AccountController : Controller
    {
        private static string sentCode = "";

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
            return RedirectToAction("Login");
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
    }
}

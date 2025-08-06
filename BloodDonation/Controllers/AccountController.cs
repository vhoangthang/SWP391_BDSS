using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;

namespace BloodDonation.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SmtpSettings _smtpSettings;
        private static string sentCode = "";
        private static string resetCode = "";
        private static string resetEmail = "";

        public AccountController(AppDbContext context, IOptions<SmtpSettings> smtpOptions)
        {
            _context = context;
            _smtpSettings = smtpOptions.Value;
        }

        [HttpGet]
        public IActionResult Register()
        {
            var model = new LoginRegisterViewModel();
            return View("~/Views/Login/Index.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ShowRegister = true;
                var vm = new LoginRegisterViewModel { Register = model, Login = new LoginViewModel() };
                return View("~/Views/Login/Index.cshtml", vm);
            }

            if (model.VerificationCode != sentCode)
            {
                ViewBag.ShowRegister = true;
                ModelState.AddModelError("", "Mã xác minh không đúng.");
                var vm = new LoginRegisterViewModel { Register = model, Login = new LoginViewModel() };
                return View("~/Views/Login/Index.cshtml", vm);
            }

            if (_context.Accounts.Any(a => a.Username == model.Username))
            {
                ViewBag.ShowRegister = true;
                ViewData["UsernameError"] = "Tên đăng nhập đã tồn tại.";
                var vm = new LoginRegisterViewModel { Register = model, Login = new LoginViewModel() };
                return View("~/Views/Login/Index.cshtml", vm);
            }

            if (_context.Accounts.Any(a => a.Email == model.Email))
            {
                ViewBag.ShowRegister = true;
                ViewData["EmailError"] = "Email đã được sử dụng.";
                var vm = new LoginRegisterViewModel { Register = model, Login = new LoginViewModel() };
                return View("~/Views/Login/Index.cshtml", vm);
            }

            var account = new Account
            {
                Username = model.Username,
                Password = model.Password,
                Email = model.Email,
                Role = "Donor",
                PermissionLevel = 1
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "Đăng ký thành công!";
            return RedirectToAction("Index", "Login");
        }

        [HttpPost]
        public async Task<IActionResult> SendVerificationCode([FromBody] EmailRequest request)
        {
            string email = request.Email;
            sentCode = new Random().Next(100000, 999999).ToString();

            var smtpClient = new SmtpClient(_smtpSettings.Host)
            {
                Port = _smtpSettings.Port,
                Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.UserName, _smtpSettings.DisplayName),
                Subject = "Xác minh đăng ký tài khoản Donor",
                Body = $"Xin chào! \r\n" +
                $"Blood Donation Support System (Team 7 - SWP391 - SE1837) đã nhận yêu cầu đăng ký tài khoản của bạn.\r\n" +
                $"\r\n" +
                $"Mã xác minh của bạn là: {sentCode}\r\n" +
                $"\r\n" +
                $"Đây là thư tự động, vui lòng không trả lời\r\n" +
                $"Trân trọng!\r\n" +
                $"Blood Donation Support System\r\n" +
                $"(Team 7 - SWP391 - SE1837)\r\n",
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

            var smtpClient = new SmtpClient(_smtpSettings.Host)
            {
                Port = _smtpSettings.Port,
                Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.UserName, _smtpSettings.DisplayName),
                Subject = "Xác minh đặt lại mật khẩu tài khoản Donor",
                Body = $"Xin chào!\r\n" +
                $"Blood Donation Support System (Team 7 - SWP391 - SE1837) đã nhận yêu cầu đặt lại mật khẩu tài khoản của bạn.\r\n" +
                $"\r\n" +
                $"Mã xác minh của bạn là: {resetCode}\r\n" +
                $"\r\n" +
                $"Đây là thư tự động, vui lòng không trả lời\r\n" +
                $"Trân trọng!\r\n" +
                $"Blood Donation Support System\r\n" +
                $"(Team 7 - SWP391 - SE1837)\r\n",
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

            account.Password = newPassword;
            _context.SaveChanges();

            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public IActionResult CheckUsername(string username)
        {
            bool exists = _context.Accounts.Any(a => a.Username == username);
            return Json(new { exists });
        }

        [HttpGet]
        public IActionResult CheckEmail(string email)
        {
            bool exists = _context.Accounts.Any(a => a.Email == email);
            return Json(new { exists });
        }
    }
}

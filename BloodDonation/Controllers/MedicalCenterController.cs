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

                    // Thêm thông báo thành công với thông tin status
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
    }
}
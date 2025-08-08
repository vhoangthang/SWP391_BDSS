using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using BloodDonation.Data;

namespace BloodDonation.Controllers
{
    public class DonationFormController : Controller
    {
        private readonly AppDbContext _context;

        public DonationFormController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ClearBookingSession()
        {
            HttpContext.Session.Remove("AppointmentDate");
            HttpContext.Session.Remove("TimeSlot");
            HttpContext.Session.Remove("BloodTypeID");
            return Ok();
        }

        [HttpPost]
        public IActionResult SubmitSurvey([FromBody] HealthSurveyViewModel model)
        {
            var appointmentDateStr = HttpContext.Session.GetString("AppointmentDate");
            var timeSlot = HttpContext.Session.GetString("TimeSlot");
            var bloodTypeId = HttpContext.Session.GetInt32("BloodTypeID");

            if (appointmentDateStr == null || timeSlot == null || bloodTypeId == null)
                return Json(new { success = false, message = "Thiếu thông tin đặt lịch" });

            // Get DonorID
            var username = HttpContext.Session.GetString("Username");
            var account = _context.Accounts.FirstOrDefault(a => a.Username == username);
            var donor = _context.Donors.FirstOrDefault(d => d.AccountID == account.AccountID);

            if (donor == null)
                return Json(new { success = false, message = "Không tìm thấy thông tin người hiến máu, vui lòng cập nhật hồ sơ", code = "NO_DONOR" });

            // If question 10 is 'true' and the current status is false or null, update to true
            if (model.Answers.TryGetValue("10_AnhChiSanSangHienMauNeuDuDieuKien", out var q10))
            {
                donor.IsAvailable = (q10 == "true");
                _context.SaveChanges();
            }

            var now = DateTime.Now;

            // Check blood donation history in the last 14 days
            var lastAppointment = _context.DonationAppointments
                .Where(a => a.DonorID == donor.DonorID && a.Status != "Cancelled" && a.Status != "Rejected")
                .OrderByDescending(a => a.AppointmentDate)
                .FirstOrDefault();

            if (lastAppointment != null)
            {
                var nextEligibleDate = lastAppointment.AppointmentDate.AddDays(84);
                if (now < nextEligibleDate)
                {
                    return Json(new { success = false, message = $"Bạn chỉ có thể đăng ký hiến máu lại sau 12 tuần kể từ lần đăng ký gần nhất! (sau ngày {nextEligibleDate:dd/MM/yyyy})" });
                }
            }

            // Get a valid MedicalCenterID (e.g., take the first record)
            var medicalCenterId = _context.MedicalCenters.Select(m => m.MedicalCenterID).FirstOrDefault();

            // Server-side exclusion check
            var answers = model.Answers;
            if (
                (answers.TryGetValue("3_TruocDayAnhChiCoMacCacBenhLietKeKhong", out var q3) && q3 == "true") ||
                (answers.TryGetValue("4_KhoiBenhSauMacCacBenh12Thang", out var q4a) && q4a == "true") ||
                (answers.TryGetValue("4_DuocTruyenMauHoacGayGhepMo", out var q4b) && q4b == "true") ||
                (answers.TryGetValue("4_TiemVaccine", out var q4c) && q4c == "true") ||
                (answers.TryGetValue("5_KhoiBenhSauMacCacBenh6Thang", out var q5) && q5 == "true")
            )
            {
                return Json(new { success = false, message = "Bạn không đủ điều kiện hiến máu do có tiền sử bệnh lý không phù hợp." });
            }

            // Create Appointment
            var appointment = new DonationAppointment
            {
                DonorID = donor.DonorID,
                AppointmentDate = DateTime.Parse(appointmentDateStr),
                TimeSlot = timeSlot,
                BloodTypeID = bloodTypeId.Value,
                MedicalCenterID = medicalCenterId,
                Status = "Pending",
                HealthSurvey = JsonConvert.SerializeObject(model.Answers) // Save JSON (Dictionary to JSON string)
            };

            _context.DonationAppointments.Add(appointment);
            _context.SaveChanges(); // To get AppointmentID

            // Save answers to HealthSurvey table
            return Json(new { success = true, appointmentId = appointment.AppointmentID });
        }
    }
}

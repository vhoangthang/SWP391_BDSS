using BloodDonation.Data;
using BloodDonation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BloodDonation.Controllers
{
    public class DonationSummaryController : Controller
    {
        private readonly AppDbContext _context;
        private static readonly Dictionary<string, string> HealthSurveyQuestions = new()
        {
            { "1_AnhChiDaTungHienMauChua", "1. Anh/Chị từng hiến máu chưa?" },
            { "2_HienTaiAnhChiCoMacBenhLyKhong", "2. Hiện tại, anh/chị có mắc bệnh lý nào không?" },
            { "3_TruocDayAnhChiCoMacCacBenhLietKeKhong", "3. Trước đây, anh/chị có từng mắc một trong các bệnh: viêm gan siêu vi B, C, HIV, ..." },
            { "4_KhoiBenhSauMacCacBenh12Thang", "4.1 Anh/Chị có khỏi bệnh sau sốt rét, giang mai, lao, phẫu thuật không?" },
            { "4_DuocTruyenMauHoacGayGhepMo", "4.2 Anh/Chị có được truyền máu hoặc ghép mô không?" },
            { "4_TiemVaccine", "4.3 Gần đây anh/chị có tiêm vaccine không?" },
            { "5_KhoiBenhSauMacCacBenh6Thang", "5. Trong 06 tháng gần đây, anh/chị có khỏi bệnh sau các bệnh truyền nhiễm, viêm tủy không?" },
            { "6_KhoiBenhSauMacCacBenh1Thang", "6. Trong 01 tháng gần đây, anh/chị có khỏi bệnh viêm tiết niệu, viêm phổi, rubella không?" },
            { "7_BiCumCamLanhHoNhucDauSotDauHong14Ngay", "7. Trong 14 ngày gần đây, anh/chị có bị cảm, sốt, đau họng không?" },
            { "8_DungThuocKhangSinhKhangViêmAspirinCorticoide7Ngay", "8. Trong 07 ngày gần đây, anh/chị có dùng thuốc kháng sinh, aspirin, corticoid không?" },
            { "9_DangMangThaiHoacNuoiConDuoi12Thang", "9.1 Đang mang thai hoặc nuôi con dưới 12 tháng tuổi?" },
            { "9_ChamDutThaiKy12ThangGanDay", "9.2 Chấm dứt thai kỳ trong 12 tháng gần đây?" },
            { "10_AnhChiSanSangHienMauNeuDuDieuKien", "10. Anh/chị có sẵn sàng hiến máu mọi lúc khi cần không?" }
        };

        public DonationSummaryController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Login");
            }

            var account = _context.Accounts.FirstOrDefault(a => a.Username == username);
            if (account == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var donor = _context.Donors
                .Include(d => d.Account)
                .FirstOrDefault(d => d.AccountID == account.AccountID);

            DonationSummaryViewModel viewModel;

            if (donor == null)
            {
                TempData["NeedUpdateProfile"] = true;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                var appointments = _context.DonationAppointments
                    .Include(a => a.BloodType)
                    .Include(a => a.MedicalCenter)
                    .Where(a => a.DonorID == donor.DonorID)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToList();

                ViewBag.HealthSurveyQuestions = HealthSurveyQuestions;

                viewModel = new DonationSummaryViewModel
                {
                    Appointments = appointments,
                    Donor = donor
                };
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int appointmentId)
        {
            var appointment = _context.DonationAppointments.FirstOrDefault(a => a.AppointmentID == appointmentId);
            if (appointment != null)
            {
                if (appointment.Status == "Completed")
                {
                    // Do not delete, return a message or simply do nothing
                    return RedirectToAction("Index");
                }
                else if (appointment.Status == "Pending")
                {
                    // Delete as before
                    var surveys = _context.HealthSurveys
                        .Where(h => h.AppointmentID != null && h.AppointmentID == appointmentId)
                        .ToList();
                    if (surveys.Any())
                    {
                        _context.HealthSurveys.RemoveRange(surveys);
                    }
                       
                    var donor = _context.Donors.FirstOrDefault(d => d.DonorID == appointment.DonorID);
                    if (donor != null && donor.IsAvailable == true)
                    {
                        donor.IsAvailable = false;
                        _context.SaveChanges();
                    }
                    
                    _context.DonationAppointments.Remove(appointment);
                    _context.SaveChanges();
                }
                else if (appointment.Status == "Confirmed")
                {
                    // Change status to Cancelled
                    appointment.Status = "Cancelled";
                    // If donor is available, change to not available
                    var donor = _context.Donors.FirstOrDefault(d => d.DonorID == appointment.DonorID);
                    if (donor != null && donor.IsAvailable == true)
                    {
                        donor.IsAvailable = false;
                        _context.SaveChanges();
                    }
                    _context.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }
    }
}
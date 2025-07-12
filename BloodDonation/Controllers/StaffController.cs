using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BloodDonation.Data;
using BloodDonation.Models;
using System;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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
                    .ThenInclude(d => d.Account)
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

                        // Nếu donor đang sẵn sàng thì chuyển về không sẵn sàng
                        var donorEntity = _context.Donors.FirstOrDefault(d => d.DonorID == appointment.DonorID);
                        if (donorEntity != null && donorEntity.IsAvailable == true)
                        {
                            donorEntity.IsAvailable = false;
                            _context.SaveChanges();
                        }

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

                // Thêm notification nếu trạng thái là Confirmed hoặc Rejected
                if (appointment.Status == "Confirmed" || appointment.Status == "Rejected")
                {
                    var notification = new Notification
                    {
                        DonorID = appointment.DonorID,
                        Message = $"Đơn hiến máu của bạn đã chuyển sang trạng thái: {appointment.Status}",
                        SentAt = DateTime.Now,
                        IsRead = false,
                        Type = "AppointmentStatus",
                        IsConfirmed = false,
                        AccountID = null
                    };
                    _context.Notifications.Add(notification);
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

        // Hàm lọc nhóm máu tương hợp
        public static List<string> GetCompatibleBloodTypes(string recipientBloodType)
        {
            var allTypes = new List<string> { "O-", "O+", "A-", "A+", "B-", "B+", "AB-", "AB+" };
            var compatible = new List<string>();
            switch (recipientBloodType)
            {
                case "A+":
                    compatible.AddRange(new[] { "A+", "A-", "O+", "O-" });
                    break;
                case "A-":
                    compatible.AddRange(new[] { "A-", "O-" });
                    break;
                case "B+":
                    compatible.AddRange(new[] { "B+", "B-", "O+", "O-" });
                    break;
                case "B-":
                    compatible.AddRange(new[] { "B-", "O-" });
                    break;
                case "AB+":
                    compatible.AddRange(allTypes); // Nhận được tất cả
                    break;
                case "AB-":
                    compatible.AddRange(new[] { "AB-", "A-", "B-", "O-" });
                    break;
                case "O+":
                    compatible.AddRange(new[] { "O+", "O-" });
                    break;
                case "O-":
                    compatible.Add("O-");
                    break;
            }
            return compatible;
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
                return NotFound();

            // Lấy danh sách nhóm máu tương hợp
            var compatibleTypes = GetCompatibleBloodTypes(request.BloodType?.Type);
            // Nếu chỉ có đúng 1 nhóm máu tương hợp và đó là nhóm máu được yêu cầu, chỉ cho chọn đúng nhóm máu đó
            if (compatibleTypes.Count == 1 && compatibleTypes[0] == request.BloodType?.Type)
            {
                var onlyType = _context.BloodTypes.Where(bt => bt.Type == request.BloodType.Type).ToList();
                ViewBag.CompatibleBloodTypes = onlyType;
            }
            else
            {
                var compatibleBloodTypes = _context.BloodTypes
                    .Where(bt => compatibleTypes.Contains(bt.Type))
                    .ToList();
                ViewBag.CompatibleBloodTypes = compatibleBloodTypes;
            }

            return View(request);
        }

        [HttpPost]
        public IActionResult CompleteBloodRequest(int id, string note, int SelectedBloodTypeID)
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

            // Kiểm tra nhóm máu được chọn có tương hợp không
            var compatibleTypes = GetCompatibleBloodTypes(request.BloodType?.Type);
            var selectedBloodType = _context.BloodTypes.FirstOrDefault(bt => bt.BloodTypeID == SelectedBloodTypeID);
            if (selectedBloodType == null || !compatibleTypes.Contains(selectedBloodType.Type))
            {
                TempData["Error"] = "❌ Nhóm máu được chọn không tương hợp với yêu cầu.";
                return RedirectToAction("ProcessBloodRequest", new { id });
            }

            int bloodBankId = 1; // Mặc định
            var inventory = _context.BloodInventories
                .FirstOrDefault(b => b.BloodTypeID == SelectedBloodTypeID && b.BloodBankID == bloodBankId);

            if (inventory != null && inventory.Quantity >= request.Quantity)
            {
                inventory.Quantity -= request.Quantity;
                inventory.LastUpdated = DateTime.Now;

                request.Status = "Completed";
                request.BloodGiven = selectedBloodType.Type;
                TempData["Message"] = "✅ Yêu cầu đã được xử lý và cập nhật kho máu.";
            }
            else
            {
                request.Status = "Pending";
                TempData["Info"] = "⚠️ Kho máu không đủ. Yêu cầu chuyển sang trạng thái chờ.";
            }

            _context.SaveChanges();
            return RedirectToAction("BloodRequestDetails");
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

        // Hiển thị chi tiết yêu cầu máu
        public IActionResult BloodRequestDetails(int id)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");

            var bloodRequest = _context.BloodRequests
                .Include(x => x.MedicalCenter)
                .Include(x => x.BloodType)
                .FirstOrDefault(x => x.BloodRequestID == id);

            if (bloodRequest == null)
            {
                return NotFound();
            }

            // Lấy thông tin kho máu cho nhóm máu yêu cầu
            var bloodInventory = _context.BloodInventories
                .Include(b => b.BloodType)
                .Include(b => b.BloodBank)
                .FirstOrDefault(b => b.BloodTypeID == bloodRequest.BloodTypeID);

            // Lấy tất cả nhóm máu để chọn loại tương hợp
            var allBloodTypes = _context.BloodTypes.ToList();

            // Lấy thông tin kho máu cho tất cả nhóm máu
            var allBloodInventories = _context.BloodInventories
                .Include(b => b.BloodType)
                .Include(b => b.BloodBank)
                .ToList();

            // Lấy người hiến máu gần nhất (cùng nhóm máu, sẵn sàng)
            var nearbyDonors = _context.Donors
                .Include(d => d.Account)
                .Where(d => d.BloodTypeID == bloodRequest.BloodTypeID && d.IsAvailable == true)
                .OrderByDescending(d => d.IsAvailable)
                .Take(5)
                .ToList();
            ViewBag.NearbyDonors = nearbyDonors;

            ViewBag.BloodInventory = bloodInventory;
            ViewBag.AllBloodTypes = allBloodTypes;
            ViewBag.AllBloodInventories = allBloodInventories;

            // KHÔNG tính lại IsCompatible, chỉ lấy từ DB
            bool isCompatible = bloodRequest.IsCompatible;
            ViewBag.IsCompatible = isCompatible;
            if (!isCompatible)
            {
                var onlyType = _context.BloodTypes.Where(bt => bt.Type == bloodRequest.BloodType.Type).ToList();
                ViewBag.CompatibleBloodTypes = onlyType;
            }
            else
            {
                var compatibleTypes = GetCompatibleBloodTypes(bloodRequest.BloodType?.Type);
                var compatibleBloodTypes = _context.BloodTypes
                    .Where(bt => compatibleTypes.Contains(bt.Type))
                    .ToList();
                ViewBag.CompatibleBloodTypes = compatibleBloodTypes;
            }

            return View(bloodRequest);
        }

        // Xử lý yêu cầu máu trực tiếp từ trang chi tiết
        [HttpPost]
        public IActionResult ProcessBloodRequestFromDetails(int bloodRequestId, string action, int? selectedBloodTypeId = null, decimal? quantity = null)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");

            var bloodRequest = _context.BloodRequests
                .Include(x => x.MedicalCenter)
                .Include(x => x.BloodType)
                .FirstOrDefault(x => x.BloodRequestID == bloodRequestId);

            if (bloodRequest == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu máu.";
                return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
            }

            // Lấy danh sách nhóm máu tương thích
            var compatibleTypes = GetCompatibleBloodTypes(bloodRequest.BloodType?.Type);
            bool isCompatible = !(compatibleTypes.Count == 1 && compatibleTypes[0] == bloodRequest.BloodType?.Type);
            ViewBag.IsCompatible = isCompatible;

            switch (action.ToLower())
            {
                case "approve":
                    bloodRequest.Status = "Approved";
                    TempData["Message"] = "Yêu cầu đã được duyệt.";
                    break;
                case "reject":
                    bloodRequest.Status = "Rejected";
                    TempData["Message"] = "Yêu cầu đã bị từ chối.";
                    break;
                case "complete":
                    // Kiểm tra nhóm máu được chọn có hợp lệ không
                    if (!selectedBloodTypeId.HasValue)
                    {
                        TempData["Error"] = "Vui lòng chọn nhóm máu để cấp phát.";
                        return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
                    }
                    var selectedBloodType = _context.BloodTypes.FirstOrDefault(bt => bt.BloodTypeID == selectedBloodTypeId.Value);
                    if (selectedBloodType == null)
                    {
                        TempData["Error"] = "Nhóm máu không hợp lệ.";
                        return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
                    }
                    if (!isCompatible)
                    {
                        // Chỉ cho phép chọn đúng nhóm máu yêu cầu
                        if (selectedBloodType.Type != bloodRequest.BloodType.Type)
                        {
                            TempData["Error"] = "Chỉ được chọn đúng nhóm máu yêu cầu.";
                            return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
                        }
                    }
                    else
                    {
                        // Phải nằm trong danh sách tương hợp
                        if (!compatibleTypes.Contains(selectedBloodType.Type))
                        {
                            TempData["Error"] = "Nhóm máu không tương thích.";
                            return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
                        }
                    }
                    // Trừ kho máu
                    if (quantity == null || quantity <= 0) quantity = bloodRequest.Quantity;
                    var inventory = _context.BloodInventories.FirstOrDefault(b => b.BloodTypeID == selectedBloodType.BloodTypeID);
                    if (inventory != null && inventory.Quantity >= quantity)
                    {
                        inventory.Quantity -= quantity.Value;
                        inventory.LastUpdated = DateTime.Now;
                        bloodRequest.Status = "Completed";
                        bloodRequest.BloodGiven = selectedBloodType.Type;
                        TempData["Message"] = "Yêu cầu đã được hoàn thành và kho máu đã được cập nhật.";
                    }
                    else
                    {
                        bloodRequest.Status = "Pending";
                        TempData["Error"] = "Kho máu không đủ. Đơn chuyển sang trạng thái chờ.";
                    }
                    break;
            }

            _context.SaveChanges();
            return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
        }

        // Hiển thị danh sách người hiến máu gần nhất
        public async Task<IActionResult> NearestDonors(int? bloodBankId = null, string customAddress = null)
        {
            var bloodBanks = _context.BloodBanks.ToList();
            ViewBag.BloodBanks = bloodBanks;
            ViewBag.CustomAddress = customAddress;

            int selectedBankId;
            if (bloodBanks.Count == 1)
            {
                selectedBankId = bloodBanks.First().BloodBankID;
            }
            else if (bloodBankId.HasValue)
            {
                selectedBankId = bloodBankId.Value;
            }
            else
            {
                // Chưa chọn blood bank, chỉ hiển thị form chọn
                ViewBag.SelectedBankId = null;
                ViewBag.Donors = null;
                return View();
            }

            ViewBag.SelectedBankId = selectedBankId;

            // Lấy danh sách donor sẵn sàng có địa chỉ
            var donors = _context.Donors
                .Include(d => d.BloodType)
                .Where(d => d.IsAvailable == true && !string.IsNullOrEmpty(d.Address))
                .ToList();

            // Lấy địa chỉ kho máu (ưu tiên customAddress nếu có)
            string bankLocation;
            if (!string.IsNullOrWhiteSpace(customAddress))
            {
                bankLocation = customAddress;
            }
            else
            {
                var bloodBank = bloodBanks.FirstOrDefault(b => b.BloodBankID == selectedBankId);
                if (bloodBank == null || string.IsNullOrEmpty(bloodBank.Location))
                {
                    ViewBag.Donors = null;
                    ViewBag.Error = "Không tìm thấy địa chỉ kho máu.";
                    return View();
                }
                bankLocation = bloodBank.Location;
            }

            // Gọi API geocode để lấy tọa độ cho bankLocation và donor
            var apiKey = "5b3ce3597851110001cf62484675ccea183f4166ab762b8429b80eb8";
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            async Task<(double lat, double lon)?> GeocodeAsync(string address)
            {
                var url = $"https://api.openrouteservice.org/geocode/search?api_key={apiKey}&text={Uri.EscapeDataString(address)}&boundary.country=VN";
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var features = doc.RootElement.GetProperty("features");
                if (features.GetArrayLength() == 0) return null;
                var coords = features[0].GetProperty("geometry").GetProperty("coordinates");
                double lon = coords[0].GetDouble();
                double lat = coords[1].GetDouble();
                return (lat, lon);
            }

            // Lấy tọa độ kho máu (hoặc customAddress)
            var bankCoord = await GeocodeAsync(bankLocation);
            if (bankCoord == null)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Không lấy được tọa độ kho máu.";
                return View();
            }

            // Lấy tọa độ các donor
            var geocodeTasks = donors.Select(async donor =>
            {
                var coord = await GeocodeAsync(donor.Address);
                return (donor, coord);
            }).ToList();

            // Chờ tất cả task hoàn tất
            var donorCoords = (await Task.WhenAll(geocodeTasks)).ToList();


            // Lọc donor có tọa độ hợp lệ
            var validDonors = donorCoords.Where(x => x.coord != null).ToList();
            if (!validDonors.Any())
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Không có người hiến máu nào có tọa độ hợp lệ.";
                return View();
            }

            // Gọi matrix API để tính khoảng cách
            var locations = new List<double[]>();
            // Vị trí đầu là kho máu
            locations.Add(new double[] { bankCoord.Value.lon, bankCoord.Value.lat });
            // Các vị trí donor
            locations.AddRange(validDonors.Select(x => new double[] { x.coord.Value.lon, x.coord.Value.lat }));

            var matrixBody = new
            {
                locations = locations,
                metrics = new[] { "distance" },
                units = "km"
            };
            var matrixContent = new StringContent(JsonSerializer.Serialize(matrixBody), System.Text.Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
            var matrixResponse = await httpClient.PostAsync("https://api.openrouteservice.org/v2/matrix/driving-car", matrixContent);
            if (!matrixResponse.IsSuccessStatusCode)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Không thể tính khoảng cách (matrix API).";
                return View();
            }
            var matrixJson = await matrixResponse.Content.ReadAsStringAsync();
            using var matrixDoc = JsonDocument.Parse(matrixJson);
            var distances = matrixDoc.RootElement.GetProperty("distances");
            // Dòng đầu là từ kho máu đến các donor
            var distanceArr = distances[0];

            // Ghép donor với khoảng cách
            var donorWithDistance = validDonors.Select((x, idx) => new { Donor = x.donor, Distance = distanceArr[idx + 1].GetDouble() }).OrderBy(x => x.Distance).ToList();
            ViewBag.DonorDistances = donorWithDistance;
            ViewBag.Donors = donorWithDistance.Select(x => x.Donor).ToList();
            ViewBag.Error = null;
            return View();
        }

        // Hiển thị danh sách người hiến máu gần nhất trong bán kính 20km
        public async Task<IActionResult> NearestDonorsWithin20km(int? bloodBankId = null, string customAddress = null, int? bloodRequestId = null)
        {
            var bloodBanks = _context.BloodBanks.ToList();
            ViewBag.BloodBanks = bloodBanks;
            ViewBag.CustomAddress = customAddress;
            ViewBag.BloodRequestId = bloodRequestId;

            int selectedBankId;
            if (bloodBanks.Count == 1)
            {
                selectedBankId = bloodBanks.First().BloodBankID;
            }
            else if (bloodBankId.HasValue)
            {
                selectedBankId = bloodBankId.Value;
            }
            else
            {
                ViewBag.SelectedBankId = null;
                ViewBag.Donors = null;
                return View();
            }

            ViewBag.SelectedBankId = selectedBankId;

            // Default: all available donors with address
            var donorsQuery = _context.Donors.Include(d => d.BloodType).Where(d => d.IsAvailable == true && !string.IsNullOrEmpty(d.Address));

            // Nếu có bloodRequestId thì lọc theo tương hợp
            if (bloodRequestId.HasValue)
            {
                var bloodRequest = _context.BloodRequests.Include(r => r.BloodType).FirstOrDefault(r => r.BloodRequestID == bloodRequestId.Value);
                if (bloodRequest != null)
                {
                    if (bloodRequest.IsCompatible)
                    {
                        // Lấy các nhóm máu tương hợp
                        var compatibleTypes = GetCompatibleBloodTypes(bloodRequest.BloodType?.Type);
                        donorsQuery = donorsQuery.Where(d => compatibleTypes.Contains(d.BloodType.Type));
                    }
                    else
                    {
                        // Chỉ lấy đúng nhóm máu cần
                        donorsQuery = donorsQuery.Where(d => d.BloodType.Type == bloodRequest.BloodType.Type);
                    }
                }
            }

            var donors = donorsQuery.ToList();

            string bankLocation;
            if (!string.IsNullOrWhiteSpace(customAddress))
            {
                bankLocation = customAddress;
            }
            else
            {
                var bloodBank = bloodBanks.FirstOrDefault(b => b.BloodBankID == selectedBankId);
                if (bloodBank == null || string.IsNullOrEmpty(bloodBank.Location))
                {
                    ViewBag.Donors = null;
                    ViewBag.Error = "Không tìm thấy địa chỉ kho máu.";
                    return View();
                }
                bankLocation = bloodBank.Location;
            }

            var apiKey = "5b3ce3597851110001cf62484675ccea183f4166ab762b8429b80eb8";
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            async Task<(double lat, double lon)?> GeocodeAsync(string address)
            {
                var url = $"https://api.openrouteservice.org/geocode/search?api_key={apiKey}&text={Uri.EscapeDataString(address)}&boundary.country=VN";
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var features = doc.RootElement.GetProperty("features");
                if (features.GetArrayLength() == 0) return null;
                var coords = features[0].GetProperty("geometry").GetProperty("coordinates");
                double lon = coords[0].GetDouble();
                double lat = coords[1].GetDouble();
                return (lat, lon);
            }

            var bankCoord = await GeocodeAsync(bankLocation);
            if (bankCoord == null)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Không lấy được tọa độ kho máu.";
                return View();
            }

            var geocodeTasks = donors.Select(async donor =>
            {
                var coord = await GeocodeAsync(donor.Address);
                return (donor, coord);
            }).ToList();

            var donorCoords = (await Task.WhenAll(geocodeTasks)).ToList();

            var validDonors = donorCoords.Where(x => x.coord != null).ToList();
            if (!validDonors.Any())
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Không có người hiến máu nào có tọa độ hợp lệ.";
                return View();
            }

            var locations = new List<double[]>();
            locations.Add(new double[] { bankCoord.Value.lon, bankCoord.Value.lat });
            locations.AddRange(validDonors.Select(x => new double[] { x.coord.Value.lon, x.coord.Value.lat }));

            var matrixBody = new
            {
                locations = locations,
                metrics = new[] { "distance" },
                units = "km"
            };
            var matrixContent = new StringContent(JsonSerializer.Serialize(matrixBody), System.Text.Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
            var matrixResponse = await httpClient.PostAsync("https://api.openrouteservice.org/v2/matrix/driving-car", matrixContent);
            if (!matrixResponse.IsSuccessStatusCode)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Không thể tính khoảng cách (matrix API).";
                return View();
            }
            var matrixJson = await matrixResponse.Content.ReadAsStringAsync();
            using var matrixDoc = JsonDocument.Parse(matrixJson);
            var distances = matrixDoc.RootElement.GetProperty("distances");
            var distanceArr = distances[0];

            // Filter only donors within 20km
            var donorWithDistance = validDonors.Select((x, idx) => new { Donor = x.donor, Distance = distanceArr[idx + 1].GetDouble() })
                .Where(x => x.Distance <= 25)
                .OrderBy(x => x.Distance)
                .ToList();
            ViewBag.DonorDistances = donorWithDistance;
            ViewBag.Donors = donorWithDistance.Select(x => x.Donor).ToList();
            ViewBag.Error = null;
            return View();
        }
    }
}

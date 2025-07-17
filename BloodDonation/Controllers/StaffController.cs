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

        // Helper method to set notification badge for staff
        private void SetStaffNotificationBadge()
        {
            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                var staffAccount = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Role.ToLower() == "staff");
                if (staffAccount != null)
                {
                    int unreadCount = _context.Notifications.Count(n => n.AccountID == staffAccount.AccountID && !n.IsRead);
                    ViewBag.UnreadNotificationCount = unreadCount;
                }
            }
        }

        // Display the list of blood donation registration requests
        public IActionResult DonationList()
        {
            SetStaffNotificationBadge();
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
            SetStaffNotificationBadge();
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
            // Get the appointment by ID (AppointmentID)
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

            // Parse JSON from HealthSurvey string if available
            Dictionary<string, object> healthSurvey = new();

            if (!string.IsNullOrEmpty(appointment.HealthSurvey))
            {
                try
                {
                    healthSurvey = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(appointment.HealthSurvey);
                }
                catch (Exception ex)
                {
                    // Log if JSON parsing fails (optional handling)
                    Console.WriteLine("Error parsing JSON: " + ex.Message);
                }
            }

            ViewData["HealthSurvey"] = healthSurvey;
            ViewData["Menu"] = "donor";

            return View("DonorRequestDetails", appointment);
        }

        public IActionResult BloodInventory()
        {
            SetStaffNotificationBadge();
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
            var inventories = _context.BloodInventories
                .Include(b => b.BloodType)
                .ToList();

            // Calculate the total blood quantity
            decimal totalQuantity = inventories.Sum(b => b.Quantity);

            // Pass to View via ViewBag or ViewModel
            ViewBag.TotalQuantity = totalQuantity;

            return View(inventories);
        }

        //Confirm donation request and update blood inventory
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
                    TempData["Error"] = "❌ Appointment not found.";
                    return RedirectToAction("DonationList");
                }

                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    TempData["Error"] = "⚠️ Appointment is already completed or cancelled. Cannot modify.";
                    return RedirectToAction("DonationList");
                }

                string normalizedAction = action?.ToLowerInvariant();
                bool isEligible = IsEligible == "true";

                switch (normalizedAction)
                {
                    case "completed":
                        if (appointment.Status == "Completed")
                        {
                            TempData["Error"] = "⚠️ Appointment was already completed.";
                            return RedirectToAction("DonationList");
                        }

                        if (appointment.BloodTypeID <= 0)
                        {
                            TempData["Error"] = "⚠️ Missing blood type information.";
                            return RedirectToAction("DonationList");
                        }

                        if (QuantityDonated <= 0)
                        {
                            TempData["Error"] = "⚠️ Blood donation quantity must be greater than 0.";
                            return RedirectToAction("DonationList");
                        }

                        appointment.Status = "Completed";
                        appointment.QuantityDonated = QuantityDonated;
                        appointment.AppointmentDate = DateTime.Now; // Update completion date

                        // If donor is available, set to unavailable
                        var donorEntity = _context.Donors.FirstOrDefault(d => d.DonorID == appointment.DonorID);
                        if (donorEntity != null && donorEntity.IsAvailable == true)
                        {
                            donorEntity.IsAvailable = false;
                            _context.SaveChanges();
                        }

                        // ✅ Update or create blood inventory
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
                                BloodBankID = 1, // TODO: Get correct BloodBankID if logic exists, default to 1
                                Quantity = QuantityDonated,
                                LastUpdated = DateTime.Now
                            };
                            _context.BloodInventories.Add(newInventory);
                        }

                        // *** CREATE CERTIFICATE ***
                        var existingCertificate = _context.DonationCertificates
                            .FirstOrDefault(c => c.AppointmentID == appointment.AppointmentID);
                        if (existingCertificate == null)
                        {
                            var donor = _context.Donors.FirstOrDefault(d => d.DonorID == appointment.DonorID);
                            var bloodType = _context.BloodTypes.FirstOrDefault(b => b.BloodTypeID == appointment.BloodTypeID);
                            var medicalCenter = _context.MedicalCenters.FirstOrDefault(m => m.MedicalCenterID == appointment.MedicalCenterID);

                            string details = $"Donation certificate for {(donor?.Name ?? "Donor")}, blood type {bloodType?.Type ?? "?"}, donated {appointment.QuantityDonated}CC at {medicalCenter?.Name ?? "Medical Center"} on {appointment.AppointmentDate:dd/MM/yyyy}.";

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
                        }
                        else
                        {
                            TempData["Error"] = "❌ Donor is not eligible.";
                            return RedirectToAction("DonationList");
                        }
                        break;

                    case "reject":
                        appointment.Status = "Rejected";
                        // If donor is available, set to unavailable
                        var donorReject = _context.Donors.FirstOrDefault(d => d.DonorID == appointment.DonorID);
                        if (donorReject != null && donorReject.IsAvailable == true)
                        {
                            donorReject.IsAvailable = false;
                            _context.SaveChanges();
                        }
                        break;

                    case "approve":
                        appointment.Status = "Approved";
                        break;
                    case "cancel":
                        appointment.Status = "Cancelled";
                        break;

                    default:
                        TempData["Error"] = "❌ Invalid action.";
                        return RedirectToAction("DonationList");
                }

                // Add notification if status is Confirmed or Rejected
                if (appointment.Status == "Confirmed" || appointment.Status == "Rejected")
                {
                    var donor = _context.Donors.Include(d => d.Account).FirstOrDefault(d => d.DonorID == appointment.DonorID);
                    string notificationMessage;
                    switch (appointment.Status)
                    {
                        case "Confirmed":
                            notificationMessage = "Your request has been approved.";
                            break;
                        case "Rejected":
                            notificationMessage = "Your request has been rejected.";
                            break;
                        default:
                            notificationMessage = $"Your donation request status has changed to: {appointment.Status}";
                            break;
                    }
                    var notification = new Notification
                    {
                        DonorID = appointment.DonorID,
                        Message = notificationMessage,
                        SentAt = DateTime.Now,
                        IsRead = false,
                        Type = "AppointmentStatus",
                        IsConfirmed = false,
                        AccountID = donor?.AccountID // Ensure correct AccountID is always assigned
                    };
                    _context.Notifications.Add(notification);
                }

                // ✅ Save changes
                _context.SaveChanges();
                TempData["Message"] = "✅ Status updated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ An error occurred: {ex.Message}";
                Console.WriteLine($"[ERROR] ConfirmDonation: {ex.Message}");
            }

            return RedirectToAction("DonationList");
        }

        public IActionResult BloodRequestList()
        {
            SetStaffNotificationBadge();
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
            var approvedRequests = _context.BloodRequests
                .Include(r => r.MedicalCenter)
                .Include(r => r.BloodType)
                //.Where(r => r.Status == "Approved")
                .ToList();

            return View(approvedRequests);
        }

        // Function to filter compatible blood types
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
                    compatible.AddRange(allTypes); // Accept all
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
            SetStaffNotificationBadge();
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
            var request = _context.BloodRequests
                .Include(r => r.MedicalCenter)
                .Include(r => r.BloodType)
                .FirstOrDefault(r => r.BloodRequestID == id);

            if (request == null || request.Status == "Completed" || request.Status == "Rejected")
                return NotFound();

            // Get the list of compatible blood types
            var compatibleTypes = GetCompatibleBloodTypes(request.BloodType?.Type);
            // If only one compatible blood type and it matches the request, only allow that type
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
            SetStaffNotificationBadge();
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");
            var request = _context.BloodRequests
                .Include(r => r.BloodType)
                .FirstOrDefault(r => r.BloodRequestID == id);

            if (request == null)
            {
                TempData["Error"] = "❌ Request not found.";
                return RedirectToAction("BloodRequestList");
            }

            // ❌ Nếu trạng thái không phải Approved, không xử lý
            // If status is not Approved, do not process
            if (!string.Equals(request.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "⚠️ Only process requests with 'Approved' status.";
                return RedirectToAction("BloodRequestList");
            }

            // Kiểm tra nếu nhóm máu được chọn tương hợp
            // Check if the selected blood type is compatible
            var compatibleTypes = GetCompatibleBloodTypes(request.BloodType?.Type);
            var selectedBloodType = _context.BloodTypes.FirstOrDefault(bt => bt.BloodTypeID == SelectedBloodTypeID);
            if (selectedBloodType == null || !compatibleTypes.Contains(selectedBloodType.Type))
            {
                TempData["Error"] = "❌ Selected blood type is not compatible with the request.";
                return RedirectToAction("ProcessBloodRequest", new { id });
            }

            int bloodBankId = 1; // Default
            var inventory = _context.BloodInventories
                .FirstOrDefault(b => b.BloodTypeID == SelectedBloodTypeID && b.BloodBankID == bloodBankId);

            if (inventory != null && inventory.Quantity >= request.Quantity)
            {
                inventory.Quantity -= request.Quantity;
                inventory.LastUpdated = DateTime.Now;

                request.Status = "Completed";
                request.BloodGiven = selectedBloodType.Type;
                TempData["Message"] = "✅ Request processed and blood inventory updated.";
            }
            else
            {
                request.Status = "Pending";
                TempData["Info"] = "⚠️ Blood inventory insufficient. Request transferred to pending state.";
            }

            _context.SaveChanges();
            return RedirectToAction("BloodRequestDetails");
        }

        public IActionResult AppointmentDetail(int id)
        {
            SetStaffNotificationBadge();
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

            // Flag to determine if processing is allowed
            ViewBag.AllowEdit = appointment.Status == "Pending" || appointment.Status == "Confirmed";

            // If you need to send the survey again
            var surveyDict = JsonSerializer.Deserialize<Dictionary<string, bool>>(appointment.HealthSurvey ?? "{}");
            ViewData["HealthSurvey"] = surveyDict;

            return View(appointment);
        }

        // Display blood request details
        public IActionResult BloodRequestDetails(int id)
        {
            SetStaffNotificationBadge();
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

            // Get blood inventory info for the requested blood type
            var bloodInventory = _context.BloodInventories
                .Include(b => b.BloodType)
                .Include(b => b.BloodBank)
                .FirstOrDefault(b => b.BloodTypeID == bloodRequest.BloodTypeID);

            // Get all blood types to select compatible types
            var allBloodTypes = _context.BloodTypes.ToList();

            // Get blood inventory info for all blood types
            var allBloodInventories = _context.BloodInventories
                .Include(b => b.BloodType)
                .Include(b => b.BloodBank)
                .ToList();

            // Get nearest available donors with the same blood type
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

            // DO NOT recalculate IsCompatible, just use value from DB
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

        // Handle blood request directly from details page
        [HttpPost]
        public IActionResult ProcessBloodRequestFromDetails(int bloodRequestId, string action, int? selectedBloodTypeId = null, decimal? quantity = null)
        {
            SetStaffNotificationBadge();
            if (!IsStaffLoggedIn())
                return RedirectToAction("Index", "Login");

            var bloodRequest = _context.BloodRequests
                .Include(x => x.MedicalCenter)
                .Include(x => x.BloodType)
                .FirstOrDefault(x => x.BloodRequestID == bloodRequestId);

            if (bloodRequest == null)
            {
                TempData["Error"] = "Blood request not found.";
                return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
            }

            // Get the list of compatible blood types
            var compatibleTypes = GetCompatibleBloodTypes(bloodRequest.BloodType?.Type);
            bool isCompatible = !(compatibleTypes.Count == 1 && compatibleTypes[0] == bloodRequest.BloodType?.Type);
            ViewBag.IsCompatible = isCompatible;

            switch (action.ToLower())
            {
                case "approve":
                    bloodRequest.Status = "Approved";
                    TempData["Message"] = "Request approved.";
                    break;
                case "reject":
                    bloodRequest.Status = "Rejected";
                    TempData["Message"] = "Request rejected.";
                    break;
                case "cancel":
                    bloodRequest.Status = "Canceled";
                    TempData["Message"] = "Request cancelled.";
                    break;
                case "complete":
                    // Check if selected blood type is valid
                    if (!selectedBloodTypeId.HasValue)
                    {
                        TempData["Error"] = "Please select a blood type to allocate.";
                        return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
                    }
                    var selectedBloodType = _context.BloodTypes.FirstOrDefault(bt => bt.BloodTypeID == selectedBloodTypeId.Value);
                    if (selectedBloodType == null)
                    {
                        TempData["Error"] = "Invalid blood type.";
                        return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
                    }
                    if (!isCompatible)
                    {
                        // Only allow selecting the exact requested blood type
                        if (selectedBloodType.Type != bloodRequest.BloodType.Type)
                        {
                            TempData["Error"] = "Only the exact requested blood type can be selected.";
                            return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
                        }
                    }
                    else
                    {
                        // Must be in the compatible types list
                        if (!compatibleTypes.Contains(selectedBloodType.Type))
                        {
                            TempData["Error"] = "Blood type not compatible.";
                            return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
                        }
                    }
                    // Deduct from blood inventory
                    if (quantity == null || quantity <= 0) quantity = bloodRequest.Quantity;
                    var inventory = _context.BloodInventories.FirstOrDefault(b => b.BloodTypeID == selectedBloodType.BloodTypeID);
                    if (inventory != null && inventory.Quantity >= quantity)
                    {
                        inventory.Quantity -= quantity.Value;
                        inventory.LastUpdated = DateTime.Now;
                        bloodRequest.Status = "Completed";
                        bloodRequest.BloodGiven = selectedBloodType.Type;
                        TempData["Message"] = "Request completed and blood inventory updated.";
                    }
                    else
                    {
                        bloodRequest.Status = "Pending";
                        TempData["Error"] = "Blood inventory insufficient. Request transferred to pending state.";
                    }
                    break;
            }

            _context.SaveChanges();
            return RedirectToAction("BloodRequestDetails", new { id = bloodRequestId });
        }

        // Display the list of nearest donors
        public async Task<IActionResult> NearestDonors(string locationId = null, string customAddress = null)
        {
            SetStaffNotificationBadge();
            try
            {
                var bloodBanks = _context.BloodBanks.ToList();
                var medicalCenters = _context.MedicalCenters.ToList();
                ViewBag.BloodBanks = bloodBanks;
                ViewBag.MedicalCenters = medicalCenters;
                ViewBag.CustomAddress = customAddress;

                int? selectedBankId = null;
                int? selectedMedicalCenterId = null;
                string locationType = null;
                int locationIntId = 0;

                if (!string.IsNullOrEmpty(locationId))
                {
                    if (locationId.StartsWith("bank_"))
                    {
                        locationType = "bank";
                        if (int.TryParse(locationId.Substring(5), out int id))
                        {
                            selectedBankId = id;
                            locationIntId = id;
                        }
                    }
                    else if (locationId.StartsWith("center_"))
                    {
                        locationType = "center";
                        if (int.TryParse(locationId.Substring(7), out int id))
                        {
                            selectedMedicalCenterId = id;
                            locationIntId = id;
                        }
                    }
                }

                ViewBag.SelectedBankId = selectedBankId;
                ViewBag.SelectedMedicalCenterId = selectedMedicalCenterId;

                if (selectedBankId == null && selectedMedicalCenterId == null && string.IsNullOrWhiteSpace(customAddress))
                {
                    // If no donation location selected, only show the selection form
                    ViewBag.Donors = null;
                    return View();
                }

                // Get list of available donors with address
                var donors = _context.Donors
                    .Include(d => d.BloodType)
                    .Where(d => d.IsAvailable == true 
                        && !string.IsNullOrEmpty(d.Address)
                        && _context.DonationAppointments.Any(a => a.DonorID == d.DonorID && a.Status == "Confirmed"))
                    .ToList();

                // Get donation location address (prefer customAddress if available)
                string locationAddress = null;
                if (!string.IsNullOrWhiteSpace(customAddress))
                {
                    locationAddress = customAddress;
                }
                else if (selectedBankId != null)
                {
                    var bloodBank = bloodBanks.FirstOrDefault(b => b.BloodBankID == selectedBankId);
                    if (bloodBank == null || string.IsNullOrEmpty(bloodBank.Location))
                    {
                        ViewBag.Donors = null;
                        ViewBag.Error = "Blood bank location not found.";
                        return View();
                    }
                    locationAddress = bloodBank.Location;
                }
                else if (selectedMedicalCenterId != null)
                {
                    var medicalCenter = medicalCenters.FirstOrDefault(c => c.MedicalCenterID == selectedMedicalCenterId);
                    if (medicalCenter == null || string.IsNullOrEmpty(medicalCenter.Location))
                    {
                        ViewBag.Donors = null;
                        ViewBag.Error = "Medical center location not found.";
                        return View();
                    }
                    locationAddress = medicalCenter.Location;
                }

                // Call geocode API to get coordinates for locationAddress and donors
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

                // Get coordinates for donation location (or customAddress)
                var locationCoord = await GeocodeAsync(locationAddress);
                if (locationCoord == null)
                {
                    ViewBag.Donors = null;
                    ViewBag.Error = "Could not get coordinates for blood donation location.";
                    return View();
                }

                // Get coordinates for donors
                var geocodeTasks = donors.Select(async donor =>
                {
                    var coord = await GeocodeAsync(donor.Address);
                    return (donor, coord);
                }).ToList();

                // Wait for all geocode tasks to complete
                var donorCoords = (await Task.WhenAll(geocodeTasks)).ToList();

                // Filter donors with valid coordinates
                var validDonors = donorCoords.Where(x => x.coord != null).ToList();
                if (!validDonors.Any())
                {
                    ViewBag.Donors = null;
                    ViewBag.Error = "No donors with valid coordinates found.";
                    return View();
                }

                // Call matrix API to calculate distances
                var locations = new List<double[]>();
                // First position is the donation location
                locations.Add(new double[] { locationCoord.Value.lon, locationCoord.Value.lat });
                // Donor positions
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
                    ViewBag.Error = "Could not calculate distance (matrix API).";
                    return View();
                }
                var matrixJson = await matrixResponse.Content.ReadAsStringAsync();
                using var matrixDoc = JsonDocument.Parse(matrixJson);
                var distances = matrixDoc.RootElement.GetProperty("distances");
                // First row is from donation location to donors
                var distanceArr = distances[0];

                // Combine donors with their distances
                var donorWithDistance = validDonors.Select((x, idx) => new { Donor = x.donor, Distance = distanceArr[idx + 1].GetDouble() }).OrderBy(x => x.Distance).ToList();
                ViewBag.DonorDistances = donorWithDistance;
                ViewBag.Donors = donorWithDistance.Select(x => x.Donor).ToList();
                ViewBag.Error = null;
                return View();
            }
            catch (HttpRequestException)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Could not connect to map service. Please check your network connection!";
                return View();
            }
            catch (TaskCanceledException)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Connection to map service timed out. Please try again!";
                return View();
            }
            catch (System.Net.Sockets.SocketException)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Could not connect to map service (network error). Please check your Internet connection!";
                return View();
            }
            catch (Exception)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "An unexpected error occurred while searching for nearest donors.";
                return View();
            }
        }

        // Display the list of nearest donors within a 20km radius
        public async Task<IActionResult> NearestDonorsWithin20km(int? bloodBankId = null, string customAddress = null, int? bloodRequestId = null)
        {
            SetStaffNotificationBadge();
            try
            {
                if (bloodRequestId.HasValue)
                {
                    var bloodRequest = _context.BloodRequests
                        .Include(r => r.BloodType)
                        .Include(r => r.MedicalCenter)
                        .FirstOrDefault(r => r.BloodRequestID == bloodRequestId.Value);

                    if (bloodRequest == null || bloodRequest.MedicalCenter == null)
                    {
                        ViewBag.Error = "Medical center information not found.";
                        return View();
                    }

                    ViewBag.MedicalCenter = bloodRequest.MedicalCenter;
                    ViewBag.BloodRequestId = bloodRequestId;

                    // Filter compatible donors
                    var donorsQuery = _context.Donors.Include(d => d.BloodType).Where(d => d.IsAvailable == true 
                        && !string.IsNullOrEmpty(d.Address)
                        && _context.DonationAppointments.Any(a => a.DonorID == d.DonorID && a.Status == "Confirmed"));
                    if (bloodRequest.IsCompatible)
                    {
                        var compatibleTypes = GetCompatibleBloodTypes(bloodRequest.BloodType?.Type);
                        donorsQuery = donorsQuery.Where(d => compatibleTypes.Contains(d.BloodType.Type));
                    }
                    else
                    {
                        donorsQuery = donorsQuery.Where(d => d.BloodType.Type == bloodRequest.BloodType.Type);
                    }
                    var donors = donorsQuery.ToList();

                    // Get medical center address (prefer customAddress if available)
                    string location = !string.IsNullOrWhiteSpace(customAddress) ? customAddress : bloodRequest.MedicalCenter.Location;
                    if (string.IsNullOrWhiteSpace(location))
                    {
                        ViewBag.Donors = null;
                        ViewBag.Error = "Medical center location not found.";
                        return View();
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

                    var centerCoord = await GeocodeAsync(location);
                    if (centerCoord == null)
                    {
                        ViewBag.Donors = null;
                        ViewBag.Error = "Could not get coordinates for medical center.";
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
                        ViewBag.Error = "No donors with valid coordinates found.";
                        return View();
                    }

                    var locations = new List<double[]>();
                    locations.Add(new double[] { centerCoord.Value.lon, centerCoord.Value.lat });
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
                        ViewBag.Error = "Could not calculate distance (matrix API).";
                        return View();
                    }
                    var matrixJson = await matrixResponse.Content.ReadAsStringAsync();
                    using var matrixDoc = JsonDocument.Parse(matrixJson);
                    var distances = matrixDoc.RootElement.GetProperty("distances");
                    var distanceArr = distances[0];

                    var donorWithDistance = validDonors.Select((x, idx) => new { Donor = x.donor, Distance = distanceArr[idx + 1].GetDouble() })
                        .Where(x => x.Distance <= 25)
                        .OrderBy(x => x.Distance)
                        .ToList();
                    ViewBag.DonorDistances = donorWithDistance;
                    ViewBag.Donors = donorWithDistance.Select(x => x.Donor).ToList();
                    ViewBag.Error = null;
                    return View();
                }
                else
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
                    var donorsQuery = _context.Donors.Include(d => d.BloodType).Where(d => d.IsAvailable == true 
                        && !string.IsNullOrEmpty(d.Address)
                        && _context.DonationAppointments.Any(a => a.DonorID == d.DonorID && a.Status == "Confirmed"));

                    var donors = donorsQuery.ToList();

                    // Get blood bank address (prefer customAddress if available)
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
                            ViewBag.Error = "Blood bank location not found.";
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
                        ViewBag.Error = "Could not get coordinates for blood bank.";
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
                        ViewBag.Error = "No donors with valid coordinates found.";
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
                        ViewBag.Error = "Could not calculate distance (matrix API).";
                        return View();
                    }
                    var matrixJson = await matrixResponse.Content.ReadAsStringAsync();
                    using var matrixDoc = JsonDocument.Parse(matrixJson);
                    var distances = matrixDoc.RootElement.GetProperty("distances");
                    var distanceArr = distances[0];

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
            catch (HttpRequestException)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Could not connect to map service. Please check your network connection!";
                return View();
            }
            catch (TaskCanceledException)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Connection to map service timed out. Please try again!";
                return View();
            }
            catch (System.Net.Sockets.SocketException)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "Could not connect to map service (network error). Please check your Internet connection!";
                return View();
            }
            catch (Exception)
            {
                ViewBag.Donors = null;
                ViewBag.Error = "An unexpected error occurred while searching for nearest donors.";
                return View();
            }
        }
    }
}

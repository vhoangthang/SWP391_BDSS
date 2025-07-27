using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Models
{
    public class ChangeRoleRequest
    {
        public int id { get; set; }
        public string newRole { get; set; }
        public int? medicalCenterId { get; set; }
    }
}

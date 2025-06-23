using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class BloodRequestViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string PatientName { get; set; }

        public int Age { get; set; }

        public string PhoneNumber { get; set; }

        public string Gender { get; set; }

        public string NationalId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhóm máu.")]
        public string BloodType { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lượng máu.")]
        public decimal Quantity { get; set; }

        public string Compatibility { get; set; }

        public string Reason { get; set; }

        public string ReceptionType { get; set; }
    }
}
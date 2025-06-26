using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class BloodRequestViewModel
    {

        [Required(ErrorMessage = "Vui lòng chọn nhóm máu.")]
        public string BloodType { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lượng máu.")]
        public decimal Quantity { get; set; }

        public string Compatibility { get; set; }
        
        public string Reason { get; set; }

        public string ReceptionType { get; set; }
    }
}
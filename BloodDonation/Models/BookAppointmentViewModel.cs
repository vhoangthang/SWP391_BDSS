using System;
using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class BookAppointmentViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime? AppointmentDate { get; set; }

        [Required]
        public string TimeSlot { get; set; }

        [Required]
        public int BloodTypeID { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("DonationAppointment")]
    public class DonationAppointment
    {
        [Key]
        public int AppointmentID { get; set; }

        public int DonorID { get; set; }

        [ForeignKey("DonorID")]
        public Donor Donor { get; set; }

        public int MedicalCenterID { get; set; }

        [ForeignKey("MedicalCenterID")]
        public MedicalCenter MedicalCenter { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public string TimeSlot { get; set; } // "Sáng" hoặc "Chiều"


        public int BloodTypeID { get; set; }

        [ForeignKey("BloodTypeID")]
        public BloodType BloodType { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string Status { get; set; }

        public string HealthSurvey { get; set; }

        public DonationCertificate DonationCertificate { get; set; }
    }
}

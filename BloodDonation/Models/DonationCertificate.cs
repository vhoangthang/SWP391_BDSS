using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("DonationCertificate")]
    public class DonationCertificate
    {
        [Key]
        public int CertificateID { get; set; }

        public int AppointmentID { get; set; }

        [ForeignKey("AppointmentID")]
        public DonationAppointment Appointment { get; set; }

        public DateTime IssueDate { get; set; }

        [MaxLength(250)]
        public string CertificateDetails { get; set; }
    }
}

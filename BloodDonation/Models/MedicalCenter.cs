using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    [Table("MedicalCenter")]
    public class MedicalCenter
    {
        [Key]
        public int MedicalCenterID { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Location { get; set; }

        [MaxLength(20)]
        public string ContactNumber { get; set; }

        public int BloodBankID { get; set; }

        [ForeignKey("BloodBankID")]
        public BloodBank BloodBank { get; set; }

        public ICollection<Account> Accounts { get; set; }
        public ICollection<BloodRequest> BloodRequests { get; set; }
        public ICollection<DonationAppointment> DonationAppointments { get; set; }
    }
}

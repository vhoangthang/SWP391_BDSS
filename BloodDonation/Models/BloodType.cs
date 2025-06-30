using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace BloodDonation.Models
{
    [Table("BloodType")]

    public class BloodType
    {
        [Key]
        public int BloodTypeID { get; set; }

        [Required, MaxLength(10)]
        public string Type { get; set; }

        public ICollection<Donor> Donors { get; set; }
        public ICollection<BloodRequest> BloodRequests { get; set; }
        public ICollection<DonationAppointment> DonationAppointments { get; set; }
        public ICollection<BloodInventory> BloodInventories { get; set; }
    }
}

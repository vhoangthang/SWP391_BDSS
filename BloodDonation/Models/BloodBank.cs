using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("BloodBank")]
    public class BloodBank
    {
        [Key]
        public int BloodBankID { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Location { get; set; }

        [MaxLength(20)]
        public string ContactNumber { get; set; }

        public ICollection<MedicalCenter> MedicalCenters { get; set; }
        public ICollection<BloodInventory> BloodInventories { get; set; }
    }
}

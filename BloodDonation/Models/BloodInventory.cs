using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("BloodInventory")]
    public class BloodInventory
    {

        [Key]
        public int InventoryID { get; set; }

        public int BloodTypeID { get; set; }

        public int BloodBankID { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Quantity { get; set; }

        public DateTime LastUpdated { get; set; }

        // Navigation Properties
        [ForeignKey("BloodTypeID")]
        public BloodType BloodType { get; set; }

        [ForeignKey("BloodBankID")]
        public BloodBank BloodBank { get; set; }
    }

}

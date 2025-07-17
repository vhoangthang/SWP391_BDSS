using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("DonorBloodRequest")]
    public class DonorBloodRequest
    {
        [Key]

        public int DonorBloodRequestID { get; set; }

        public int BloodRequestID { get; set; }
        public int DonorID { get; set; }

        public DateTime? DonationDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? QuantityDonated { get; set; }

        [ForeignKey("BloodRequestID")]
        public BloodRequest BloodRequest { get; set; }

        [ForeignKey("DonorID")]
        public Donor Donor { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("BloodRequest")]
    public class BloodRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BloodRequestID { get; set; }

        public int MedicalCenterID { get; set; }

        [ForeignKey("MedicalCenterID")]
        public virtual MedicalCenter MedicalCenter { get; set; }

        public int BloodTypeID { get; set; }

        [ForeignKey("BloodTypeID")]
        public virtual BloodType BloodType { get; set; }

        [Required]

        public string Reason { get; set; }

        public DateTime RequestDate { get; set; }

        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "NVARCHAR(50)")]
        [MaxLength(50)]
        public string? BloodGiven { get; set; }

        public bool IsEmergency { get; set; }

        public bool IsCompatible { get; set; }

        public string? Status { get; set; } = "Pending";

        public virtual ICollection<DonorBloodRequest> DonorBloodRequests { get; set; }
    }
}

﻿using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("Donor")]
    public class Donor
    {
        [Key]
        public int DonorID { get; set; }

        public int AccountID { get; set; }

        [ForeignKey("AccountID")]
        public Account Account { get; set; }

        public int? BloodTypeID { get; set; }

        [ForeignKey("BloodTypeID")]
        public BloodType? BloodType { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        public bool? IsAvailable { get; set; }

        [MaxLength(50)]
        public string? CCCD { get; set; }

        public ICollection<DonorBloodRequest> DonorBloodRequests { get; set; }
        public ICollection<DonationAppointment> DonationAppointments { get; set; }

        [ValidateNever]
        public ICollection<Notification> Notifications { get; set; }
    }
}

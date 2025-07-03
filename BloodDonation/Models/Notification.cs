using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public int DonorID { get; set; }

        [Required]
        [MaxLength]
        public string Message { get; set; }

        [Required]
        public DateTime SentAt { get; set; } = DateTime.Now;

        [Required]
        public bool IsRead { get; set; } = false;

        [MaxLength(50)]
        public string? Type { get; set; }

        [Required]
        public bool IsConfirmed { get; set; } = false;

        public int? AccountID { get; set; }

        public int? BloodRequestID { get; set; }

        // Navigation properties
        [ForeignKey("DonorID")]
        public Donor Donor { get; set; }

        [ForeignKey("AccountID")]
        public Account? Account { get; set; }

        [ForeignKey("BloodRequestID")]
        public BloodRequest? BloodRequest { get; set; }
    }
} 
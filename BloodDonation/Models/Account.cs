using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("Account")] // ✅ đặt ở đây
    public class Account
    {
        [Key]
        public int AccountID { get; set; }

        public int? MedicalCenterID { get; set; } // Có thể NULL

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(50)]
        public string Password { get; set; } // Nên mã hóa khi lưu thực tế

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; }

        [Required]
        public int PermissionLevel { get; set; }

        // Navigation property
        [ForeignKey("MedicalCenterID")]
        public MedicalCenter? MedicalCenter { get; set; }
        public Donor? Donor { get; set; }
    }
}

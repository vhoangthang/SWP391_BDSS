using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string VerificationCode { get; set; }
    }
}

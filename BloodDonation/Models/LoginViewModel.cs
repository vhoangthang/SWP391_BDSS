using System.ComponentModel.DataAnnotations;

namespace BloodDonation.Models
{
        public class LoginViewModel
        {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string Role { get; set; }

        public bool RememberMe { get; set; }
    }
}

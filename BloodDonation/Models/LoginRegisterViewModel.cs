using System;

namespace BloodDonation.Models
{
    public class LoginRegisterViewModel
    {
        public LoginViewModel Login { get; set; } = new();
        public RegisterViewModel Register { get; set; } = new();
    }
} 
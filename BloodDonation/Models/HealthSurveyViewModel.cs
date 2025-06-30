using Microsoft.AspNetCore.Mvc;

namespace BloodDonation.Models
{
    public class HealthSurveyViewModel
    {
        public Dictionary<string, string> Answers { get; set; } = new();
    }
}

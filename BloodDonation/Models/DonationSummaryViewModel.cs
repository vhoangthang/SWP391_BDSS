using System.Collections.Generic;

namespace BloodDonation.Models
{
    public class DonationSummaryViewModel
    {
        public DonationAppointment Appointments { get; set; }
        public Donor Donor { get; set; }
        public Dictionary<string, string> HealthSurveyAnswers { get; set; }
    }
}
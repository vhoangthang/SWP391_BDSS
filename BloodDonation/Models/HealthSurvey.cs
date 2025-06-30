using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodDonation.Models
{
    [Table("HealthSurvey")]   
    
    public class HealthSurvey
    {
        [Key]
        public int SurveyID { get; set; }

        [ForeignKey("DonationAppointment")]
        public int AppointmentID { get; set; }

        [MaxLength(500)]
        public string QuestionCode { get; set; }

        public string Answer { get; set; }

        public DonationAppointment DonationAppointment { get; set; }
    }
}

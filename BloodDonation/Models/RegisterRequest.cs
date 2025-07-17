public class RegisterRequest
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string BloodType { get; set; }
    public DateTime RegisterDate { get; set; }
    public string Status { get; set; } // "Pending", "Confirmed", "Completed"
    public string Location { get; set; }
}

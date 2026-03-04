namespace FitTime.Models;

public class ClassEnrollment
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public int ClientId { get; set; }
    public int? MembershipId { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string Status { get; set; } = "Enrolled";

    public Class Class { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public Membership? Membership { get; set; }
}

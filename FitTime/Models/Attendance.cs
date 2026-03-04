namespace FitTime.Models;

public class Attendance
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public int ClientId { get; set; }
    public int? MembershipId { get; set; }
    public DateTime CheckedInAt { get; set; }
    public int? CheckedInByUserId { get; set; }
    public string Status { get; set; } = "Present";
    public string? Notes { get; set; }

    public Class Class { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public Membership? Membership { get; set; }
    public User? CheckedInByUser { get; set; }
}

namespace FitTime.Models;

public class Membership
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int MembershipTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsUnlimited { get; set; }
    public int VisitsRemaining { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SoldByUserId { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Client Client { get; set; } = null!;
    public MembershipType MembershipType { get; set; } = null!;
    public User? SoldByUser { get; set; }
}

namespace FitTime.Models;

public class MembershipType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public bool IsUnlimited { get; set; }
    public int VisitCount { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}

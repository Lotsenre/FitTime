using System.Windows.Media;

namespace FitTime.Models;

public class ClientDisplay
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string MembershipStatus { get; set; } = "—";
    public Brush MembershipStatusColor { get; set; } = Brushes.Gray;
    public DateTime CreatedAt { get; set; }
    public Client Source { get; set; } = null!;
}

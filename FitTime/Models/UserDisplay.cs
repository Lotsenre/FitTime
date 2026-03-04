using System.Windows.Media;

namespace FitTime.Models;

public class UserDisplay
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public Brush RoleColor { get; set; } = Brushes.White;
    public Brush RoleBgColor { get; set; } = Brushes.Transparent;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = Brushes.White;
    public Brush StatusBgColor { get; set; } = Brushes.Transparent;
    public Brush BorderColor { get; set; } = Brushes.Transparent;
    public DateTime CreatedAt { get; set; }
    public User Source { get; set; } = null!;
}

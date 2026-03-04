using System.Windows.Media;

namespace FitTime.Models;

public class AttendanceDisplay
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public string DateTimeText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = Brushes.White;
    public Brush BorderColor { get; set; } = Brushes.Transparent;
    public Brush ClientNameColor { get; set; } = Brushes.White;
    public Brush DetailColor { get; set; } = Brushes.Gray;
    public Attendance Source { get; set; } = null!;
}

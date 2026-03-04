using System.Windows.Media;

namespace FitTime.Models;

public class ReportTopClassDisplay
{
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public int AttendanceCount { get; set; }
    public double LoadPercent { get; set; }
    public double LoadBarWidth { get; set; }
    public Brush LoadBarColor { get; set; } = Brushes.White;
    public string CancelPercent { get; set; } = string.Empty;
}

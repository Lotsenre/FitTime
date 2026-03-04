using System.Windows.Media;

namespace FitTime.Models;

public class ScheduleClassDisplay
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public string DateTimeText { get; set; } = string.Empty;
    public string ParticipantsText { get; set; } = string.Empty;
    public Brush ParticipantsColor { get; set; } = Brushes.White;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = Brushes.White;
    public Brush StatusBgColor { get; set; } = Brushes.Transparent;
    public Brush BorderColor { get; set; } = Brushes.Transparent;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Class Source { get; set; } = null!;

    // Calendar grid positioning
    public int DayColumn { get; set; }      // 0=Mon..6=Sun
    public double TopOffset { get; set; }   // pixels from grid top
    public double BlockHeight { get; set; } // pixels based on duration
}

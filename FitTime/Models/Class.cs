namespace FitTime.Models;

public class Class
{
    public int Id { get; set; }
    public int ClassTypeId { get; set; }
    public int TrainerId { get; set; }
    public string Room { get; set; } = "Основной зал";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxParticipants { get; set; } = 20;
    public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ClassType ClassType { get; set; } = null!;
    public User Trainer { get; set; } = null!;
    public User? CreatedByUser { get; set; }
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}

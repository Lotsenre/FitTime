namespace FitTime.Models;

public class TrainerDisplay
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Specialization { get; set; }
    public int ClassesPerMonth { get; set; }
    public User Source { get; set; } = null!;
}

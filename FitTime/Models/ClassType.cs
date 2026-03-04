namespace FitTime.Models;

public class ClassType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#2196F3";
    public bool IsActive { get; set; } = true;

    public ICollection<Class> Classes { get; set; } = new List<Class>();
}

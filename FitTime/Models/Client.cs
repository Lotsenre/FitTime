namespace FitTime.Models;

public class Client
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<ClassEnrollment> Enrollments { get; set; } = new List<ClassEnrollment>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public string FullName => $"{LastName} {FirstName} {Patronymic}".TrimEnd();
    public string ShortName => $"{LastName} {FirstName[0]}.{(Patronymic != null ? Patronymic[0] + "." : "")}";
    public string Initials => $"{LastName[0]}{FirstName[0]}";
}

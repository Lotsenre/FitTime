namespace FitTime.Models;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Specialization { get; set; }
    public bool IsActive { get; set; } = true;
    public short FailedAttempts { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Role Role { get; set; } = null!;

    public string FullName => $"{LastName} {FirstName} {Patronymic}".TrimEnd();
    public string ShortName => FirstName.Length > 0
        ? $"{LastName} {FirstName[0]}.{(Patronymic is { Length: > 0 } p ? p[0] + "." : "")}"
        : LastName;
}

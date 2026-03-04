using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;

namespace FitTime.ViewModels;

public partial class UserDialogViewModel : ObservableObject
{
    private readonly FitTimeDbContext _db;

    [ObservableProperty] private string _login = string.Empty;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private ObservableCollection<Role> _roles = new();
    [ObservableProperty] private Role? _selectedRole;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _dialogResult;

    public UserDialogViewModel(FitTimeDbContext db)
    {
        _db = db;
        _ = LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        var roles = await _db.Roles.Where(r => r.Name != "Admin").OrderBy(r => r.Name).ToListAsync();
        Roles = new ObservableCollection<Role>(roles);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(FullName)
            || SelectedRole == null || string.IsNullOrWhiteSpace(Password))
            return;

        var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lastName = parts.Length > 0 ? parts[0] : string.Empty;
        var firstName = parts.Length > 1 ? parts[1] : string.Empty;
        var patronymic = parts.Length > 2 ? parts[2] : null;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

        var user = new User
        {
            Login = Login,
            PasswordHash = passwordHash,
            RoleId = SelectedRole.Id,
            FirstName = firstName,
            LastName = lastName,
            Patronymic = patronymic,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        DialogResult = true;
    }
}

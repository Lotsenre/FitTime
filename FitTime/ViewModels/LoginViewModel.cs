using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FitTime.Data;
using FitTime.Services;
using FitTime.Views;

namespace FitTime.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string _login = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoginEnabled = true;

    public LoginViewModel(FitTimeDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
        Title = "Вход в систему";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите логин и пароль";
            return;
        }

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Login == Login);

        if (user == null)
        {
            ErrorMessage = "Неверный логин или пароль";
            Log.Warning("Login failed: user '{Login}' not found", Login);
            return;
        }

        if (!user.IsActive)
        {
            ErrorMessage = "Аккаунт заблокирован. Обратитесь к администратору";
            Log.Warning("Login failed: user '{Login}' is blocked", Login);
            return;
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash);

        if (!passwordValid)
        {
            ErrorMessage = "Неверный логин или пароль";
            Log.Warning("Login failed for '{Login}'", Login);
            return;
        }

        // Success
        user.FailedAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _currentUser.CurrentUser = user;
        Log.Information("User '{Login}' logged in successfully (role: {Role})", Login, user.Role.Name);

        var mainWindow = new MainWindow
        {
            DataContext = App.Services.GetService(typeof(MainViewModel))
        };
        Application.Current.MainWindow = mainWindow;
        mainWindow.Show();

        // Close login window
        foreach (Window window in Application.Current.Windows)
        {
            if (window is LoginWindow)
            {
                window.Close();
                break;
            }
        }
    }
}

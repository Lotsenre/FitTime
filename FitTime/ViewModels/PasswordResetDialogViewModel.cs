using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitTime.Data;
using FitTime.Models;

namespace FitTime.ViewModels;

public partial class PasswordResetDialogViewModel : ObservableObject
{
    private readonly FitTimeDbContext _db;
    private User _user = null!;

    [ObservableProperty] private string _userInfo = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _dialogResult;

    public PasswordResetDialogViewModel(FitTimeDbContext db)
    {
        _db = db;
    }

    public void LoadUser(User user)
    {
        _user = user;
        UserInfo = $"{user.Login} \u2014 {user.ShortName}";
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Введите новый пароль";
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Пароли не совпадают";
            return;
        }

        if (NewPassword.Length < 6)
        {
            ErrorMessage = "Минимум 6 символов";
            return;
        }

        try
        {
            _user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            _user.FailedAttempts = 0;
            await _db.SaveChangesAsync();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка сохранения: {ex.Message}";
        }
    }
}

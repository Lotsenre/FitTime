using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;

namespace FitTime.ViewModels;

public partial class TrainerDialogViewModel : ObservableObject
{
    private readonly FitTimeDbContext _db;
    private User? _existingUser;

    [ObservableProperty] private string _dialogTitle = "ДОБАВИТЬ ТРЕНЕРА";
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string? _patronymic;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _specialization;
    [ObservableProperty] private ObservableCollection<User> _trainerAccounts = new();
    [ObservableProperty] private User? _selectedAccount;
    [ObservableProperty] private bool _dialogResult;
    [ObservableProperty] private bool _isAccountVisible = true;

    private int? _pendingAccountId;

    public TrainerDialogViewModel(FitTimeDbContext db)
    {
        _db = db;
        _ = LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        var trainers = await _db.Users
            .Include(u => u.Role)
            .Where(u => u.Role.Name == "Trainer" && u.IsActive)
            .OrderBy(u => u.LastName)
            .ToListAsync();
        TrainerAccounts = new ObservableCollection<User>(trainers);

        if (_pendingAccountId.HasValue)
        {
            SelectedAccount = TrainerAccounts.FirstOrDefault(a => a.Id == _pendingAccountId.Value);
            _pendingAccountId = null;
        }
    }

    public void LoadExisting(User trainer)
    {
        _existingUser = trainer;
        DialogTitle = trainer.Role?.Name == "Manager" ? "РЕДАКТИРОВАТЬ МЕНЕДЖЕРА" : "РЕДАКТИРОВАТЬ ТРЕНЕРА";
        LastName = trainer.LastName;
        FirstName = trainer.FirstName;
        Patronymic = trainer.Patronymic;
        Phone = trainer.Phone;
        Email = trainer.Email;
        Specialization = trainer.Specialization;
        IsAccountVisible = trainer.Role?.Name != "Manager";

        // Если аккаунты уже загружены — ищем по Id, иначе запоминаем Id для отложенной установки
        var match = TrainerAccounts.FirstOrDefault(a => a.Id == trainer.Id);
        if (match != null)
            SelectedAccount = match;
        else
            _pendingAccountId = trainer.Id;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(FirstName)) return;

        if (_existingUser != null)
        {
            _existingUser.LastName = LastName;
            _existingUser.FirstName = FirstName;
            _existingUser.Patronymic = Patronymic;
            _existingUser.Phone = Phone;
            _existingUser.Email = Email;
            _existingUser.Specialization = Specialization;
        }
        else if (SelectedAccount != null)
        {
            SelectedAccount.LastName = LastName;
            SelectedAccount.FirstName = FirstName;
            SelectedAccount.Patronymic = Patronymic;
            SelectedAccount.Phone = Phone;
            SelectedAccount.Email = Email;
            SelectedAccount.Specialization = Specialization;
        }
        else
        {
            // Создаём нового тренера без привязки к аккаунту
            var trainerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Trainer");
            if (trainerRole == null) return;

            var login = $"trainer_{LastName.ToLower()}_{DateTime.Now:yyyyMMddHHmmss}";
            var newTrainer = new User
            {
                Login = login,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("trainer123"),
                RoleId = trainerRole.Id,
                FirstName = FirstName,
                LastName = LastName,
                Patronymic = Patronymic,
                Phone = Phone,
                Email = Email,
                Specialization = Specialization,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(newTrainer);
        }

        await _db.SaveChangesAsync();
        DialogResult = true;
    }
}

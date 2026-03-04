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
        SelectedAccount = trainer;
        IsAccountVisible = trainer.Role?.Name != "Manager";
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

        await _db.SaveChangesAsync();
        DialogResult = true;
    }
}

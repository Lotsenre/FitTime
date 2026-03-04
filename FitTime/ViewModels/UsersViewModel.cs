using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;
using FitTime.Services;
using FitTime.Views;

namespace FitTime.ViewModels;

public partial class UsersViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private ObservableCollection<UserDisplay> _users = new();
    [ObservableProperty] private UserDisplay? _selectedUser;
    [ObservableProperty] private bool _canEditSelectedUser;

    private Task _initialLoadTask = null!;

    public UsersViewModel(FitTimeDbContext db, IDialogService dialog, ICurrentUserService currentUser)
    {
        _db = db;
        _dialog = dialog;
        _currentUser = currentUser;
        Title = "Пользователи";
        _initialLoadTask = LoadAsync();
    }

    partial void OnSelectedUserChanged(UserDisplay? value)
    {
        if (value == null)
        {
            CanEditSelectedUser = false;
            return;
        }
        var isTargetAdmin = value.Source.Role?.Name == "Admin";
        var isSelf = value.Source.Id == _currentUser.CurrentUser?.Id;
        CanEditSelectedUser = !isTargetAdmin && !isSelf;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var users = await _db.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Role.Name)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var displays = users.Select(u =>
            {
                var (roleName, roleColor, roleBgColor, borderColor) = u.Role.Name switch
                {
                    "Admin" => ("АДМИНИСТРАТОР",
                        new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48)),
                        new SolidColorBrush(Color.FromRgb(0x2A, 0x18, 0x00)),
                        new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48))),
                    "Trainer" => ("ТРЕНЕР",
                        new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)),
                        new SolidColorBrush(Color.FromRgb(0x0A, 0x1A, 0x2A)),
                        new SolidColorBrush(Color.FromRgb(0x1B, 0x3A, 0x5C))),
                    "Manager" => ("МЕНЕДЖЕР",
                        new SolidColorBrush(Color.FromRgb(0x9B, 0x59, 0xB6)),
                        new SolidColorBrush(Color.FromRgb(0x1A, 0x0A, 0x2A)),
                        new SolidColorBrush(Color.FromRgb(0x3D, 0x26, 0x52))),
                    _ => ("НЕИЗВЕСТНО", Brushes.Gray, Brushes.Transparent, Brushes.Gray)
                };

                var (statusText, statusColor, statusBgColor) = u.IsActive switch
                {
                    true => ("АКТИВЕН",
                        new SolidColorBrush(Color.FromRgb(0x3D, 0xBE, 0x6A)),
                        new SolidColorBrush(Color.FromRgb(0x0A, 0x20, 0x10))),
                    false => ("ЗАБЛОКИРОВАН",
                        new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)),
                        new SolidColorBrush(Color.FromRgb(0x2A, 0x0A, 0x0A)))
                };

                return new UserDisplay
                {
                    Id = u.Id,
                    Login = u.Login,
                    FullName = u.FullName,
                    RoleName = roleName,
                    RoleColor = roleColor,
                    RoleBgColor = roleBgColor,
                    StatusText = statusText,
                    StatusColor = statusColor,
                    StatusBgColor = statusBgColor,
                    BorderColor = borderColor,
                    CreatedAt = u.CreatedAt,
                    Source = u
                };
            }).ToList();

            Users = new ObservableCollection<UserDisplay>(displays);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        var vm = new UserDialogViewModel(_db);
        var result = _dialog.ShowDialog<UserDialogWindow>(vm);
        if (result == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task EditTrainerDataAsync()
    {
        if (SelectedUser == null) return;
        var vm = new TrainerDialogViewModel(_db);
        vm.LoadExisting(SelectedUser.Source);
        var result = _dialog.ShowDialog<TrainerDialogWindow>(vm);
        if (result == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser == null) return;
        await _initialLoadTask;
        var vm = new PasswordResetDialogViewModel(_db);
        vm.LoadUser(SelectedUser.Source);
        var result = _dialog.ShowDialog<PasswordResetDialogWindow>(vm);
        if (result == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task BlockUserAsync()
    {
        if (SelectedUser == null) return;
        SelectedUser.Source.IsActive = !SelectedUser.Source.IsActive;
        await _db.SaveChangesAsync();
        await LoadAsync();
    }
}

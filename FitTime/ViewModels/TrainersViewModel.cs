using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;
using FitTime.Services;
using FitTime.Views;

namespace FitTime.ViewModels;

public partial class TrainersViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<TrainerDisplay> _trainers = new();
    [ObservableProperty] private TrainerDisplay? _selectedTrainer;

    public TrainersViewModel(FitTimeDbContext db, INavigationService nav, IDialogService dialog)
    {
        _db = db;
        _nav = nav;
        _dialog = dialog;
        Title = "Тренеры";
        _ = LoadAsync();
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var trainerRoleId = await _db.Roles
                .Where(r => r.Name == "Trainer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var trainers = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.RoleId == trainerRoleId && u.IsActive)
                .OrderBy(u => u.LastName)
                .ToListAsync();

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var classCounts = await _db.Classes
                .Where(c => c.StartTime >= monthStart && c.StartTime < monthEnd)
                .GroupBy(c => c.TrainerId)
                .Select(g => new { TrainerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TrainerId, x => x.Count);

            var displays = trainers.Select(t => new TrainerDisplay
            {
                Id = t.Id,
                FullName = t.FullName,
                Phone = t.Phone,
                Email = t.Email,
                Specialization = t.Specialization,
                ClassesPerMonth = classCounts.GetValueOrDefault(t.Id, 0),
                Source = t
            }).ToList();

            Trainers = new ObservableCollection<TrainerDisplay>(displays);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddTrainerAsync()
    {
        var vm = new TrainerDialogViewModel(_db);
        var result = _dialog.ShowDialog<TrainerDialogWindow>(vm);
        if (result == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task EditTrainerAsync()
    {
        if (SelectedTrainer == null) return;
        var vm = new TrainerDialogViewModel(_db);
        vm.LoadExisting(SelectedTrainer.Source);
        var result = _dialog.ShowDialog<TrainerDialogWindow>(vm);
        if (result == true) await LoadAsync();
    }

    [RelayCommand]
    private void ViewSchedule()
    {
        _nav.NavigateTo<ScheduleViewModel>();
    }

    [RelayCommand]
    private void ViewWorkload()
    {
        _nav.NavigateTo<ReportsViewModel>();
    }
}

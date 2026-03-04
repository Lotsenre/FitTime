using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;

namespace FitTime.ViewModels;

public partial class AttendanceViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;

    [ObservableProperty] private ObservableCollection<AttendanceDisplay> _attendances = new();
    [ObservableProperty] private AttendanceDisplay? _selectedAttendance;
    [ObservableProperty] private DateTime _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;
    [ObservableProperty] private string _clientFilter = string.Empty;
    [ObservableProperty] private string _selectedStatusFilter = "Все";
    [ObservableProperty] private int _totalCount;

    public AttendanceViewModel(FitTimeDbContext db)
    {
        _db = db;
        Title = "Посещения";
        _ = LoadAsync();
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var query = _db.Attendances
                .Include(a => a.Client)
                .Include(a => a.Class).ThenInclude(c => c.ClassType)
                .Include(a => a.Class).ThenInclude(c => c.Trainer)
                .Where(a => a.CheckedInAt >= DateFrom && a.CheckedInAt <= DateTo.AddDays(1))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(ClientFilter))
            {
                var search = ClientFilter.ToLower();
                query = query.Where(a =>
                    a.Client.LastName.ToLower().Contains(search) ||
                    a.Client.FirstName.ToLower().Contains(search));
            }

            var attendances = await query
                .OrderByDescending(a => a.CheckedInAt)
                .ToListAsync();

            var filtered = SelectedStatusFilter switch
            {
                "Отмечено" => attendances.Where(a => a.Status == "Present").ToList(),
                _ => attendances
            };

            TotalCount = attendances.Count;

            var displays = filtered.Select(a =>
            {
                var (statusText, statusColor, borderColor, clientColor, detailColor) = a.Status switch
                {
                    "Present" => ("ОТМЕЧЕНО",
                        new SolidColorBrush(Color.FromRgb(0x2D, 0x8C, 0x5A)),
                        new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42)),
                        Brushes.White,
                        new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))),
                    "Cancelled" => ("ОТМЕНЕНО",
                        new SolidColorBrush(Color.FromRgb(0xC0, 0x40, 0x40)),
                        new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x2D)),
                        new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)),
                        new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44))),
                    _ => ("НЕ ЯВИЛСЯ",
                        new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                        new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                        Brushes.White,
                        new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)))
                };

                return new AttendanceDisplay
                {
                    Id = a.Id,
                    ClientName = a.Client.ShortName,
                    ClassName = a.Class.ClassType.Name,
                    TrainerName = a.Class.Trainer.ShortName,
                    DateTimeText = a.CheckedInAt.ToString("dd.MM.yyyy  HH:mm"),
                    StatusText = statusText,
                    StatusColor = statusColor,
                    BorderColor = borderColor,
                    ClientNameColor = clientColor,
                    DetailColor = detailColor,
                    Source = a
                };
            }).ToList();

            Attendances = new ObservableCollection<AttendanceDisplay>(displays);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SetStatusFilter(string filter)
    {
        SelectedStatusFilter = filter;
    }

    [RelayCommand]
    private async Task MarkAttendanceAsync()
    {
        if (SelectedAttendance == null) return;
        SelectedAttendance.Source.Status = "Present";
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CancelAttendanceAsync()
    {
        if (SelectedAttendance == null) return;
        SelectedAttendance.Source.Status = "Cancelled";
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    partial void OnDateFromChanged(DateTime value) => _ = LoadAsync();
    partial void OnDateToChanged(DateTime value) => _ = LoadAsync();
    partial void OnClientFilterChanged(string value) => _ = LoadAsync();
    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadAsync();
}

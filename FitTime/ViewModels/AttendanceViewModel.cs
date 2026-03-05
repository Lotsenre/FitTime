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

    private List<Attendance> _cachedAttendances = new();
    private bool _isLoadingFromDb;

    public AttendanceViewModel(FitTimeDbContext db)
    {
        _db = db;
        Title = "Посещения";
        _ = LoadFromDbAsync();
    }

    private async Task LoadFromDbAsync()
    {
        if (_isLoadingFromDb) return;
        _isLoadingFromDb = true;
        IsLoading = true;
        try
        {
            _cachedAttendances = await _db.Attendances
                .AsNoTracking()
                .Include(a => a.Client)
                .Include(a => a.Class).ThenInclude(c => c.ClassType)
                .Include(a => a.Class).ThenInclude(c => c.Trainer)
                .Where(a => a.CheckedInAt >= DateFrom && a.CheckedInAt <= DateTo.AddDays(1))
                .OrderByDescending(a => a.CheckedInAt)
                .ToListAsync();

            ApplyClientFilter();
        }
        finally
        {
            IsLoading = false;
            _isLoadingFromDb = false;
        }
    }

    private void ApplyClientFilter()
    {
        var items = _cachedAttendances.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(ClientFilter))
        {
            var search = ClientFilter.Trim().ToLower();
            items = items.Where(a =>
                (a.Client?.LastName?.ToLower().Contains(search) == true) ||
                (a.Client?.FirstName?.ToLower().Contains(search) == true));
        }

        var filtered = SelectedStatusFilter switch
        {
            "Отмечено" => items.Where(a => a.Status == "Present"),
            _ => items
        };

        var list = filtered.ToList();
        TotalCount = _cachedAttendances.Count;

        var displays = list.Select(a =>
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
                ClientName = a.Client?.ShortName ?? "—",
                ClassName = a.Class?.ClassType?.Name ?? "—",
                TrainerName = a.Class?.Trainer?.ShortName ?? "—",
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

    public override async Task LoadAsync() => await LoadFromDbAsync();

    [RelayCommand]
    private void SetStatusFilter(string filter)
    {
        SelectedStatusFilter = filter;
    }

    [RelayCommand]
    private async Task MarkAttendanceAsync()
    {
        if (SelectedAttendance == null) return;
        await _db.Attendances
            .Where(a => a.Id == SelectedAttendance.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, "Present"));
        await LoadFromDbAsync();
    }

    [RelayCommand]
    private async Task CancelAttendanceAsync()
    {
        if (SelectedAttendance == null) return;
        await _db.Attendances
            .Where(a => a.Id == SelectedAttendance.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, "Cancelled"));
        await LoadFromDbAsync();
    }

    partial void OnDateFromChanged(DateTime value) => _ = LoadFromDbAsync();
    partial void OnDateToChanged(DateTime value) => _ = LoadFromDbAsync();
    partial void OnClientFilterChanged(string value) => ApplyClientFilter();
    partial void OnSelectedStatusFilterChanged(string value) => ApplyClientFilter();
}

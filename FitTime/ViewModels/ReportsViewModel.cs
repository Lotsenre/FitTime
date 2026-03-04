using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using FitTime.Data;
using FitTime.Models;

namespace FitTime.ViewModels;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;

    [ObservableProperty] private string _selectedTab = "Посещаемость";
    [ObservableProperty] private DateTime _dateFrom = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _dateTo = DateTime.Today;

    // Attendance stats
    [ObservableProperty] private int _totalAttendance;
    [ObservableProperty] private int _uniqueClients;
    [ObservableProperty] private string _avgPerDay = "0";
    [ObservableProperty] private int _cancelledCount;

    // Bar chart data (attendance by day of week)
    [ObservableProperty] private int _mondayCount;
    [ObservableProperty] private int _tuesdayCount;
    [ObservableProperty] private int _wednesdayCount;
    [ObservableProperty] private int _thursdayCount;
    [ObservableProperty] private int _fridayCount;
    [ObservableProperty] private int _saturdayCount;
    [ObservableProperty] private int _sundayCount;

    // Max value for bar scaling
    [ObservableProperty] private double _maxBarHeight = 1;

    // Top classes
    [ObservableProperty] private ObservableCollection<ReportTopClassDisplay> _topClasses = new();

    // Sales stats
    [ObservableProperty] private int _soldCount;
    [ObservableProperty] private decimal _totalRevenue;
    [ObservableProperty] private decimal _avgPrice;
    [ObservableProperty] private ObservableCollection<SalesSummaryItem> _salesByType = new();

    // Expiring memberships
    [ObservableProperty] private ObservableCollection<ExpiringMembershipItem> _expiringMemberships = new();
    [ObservableProperty] private int _expiringCount;

    private Task _initialLoadTask = null!;

    public ReportsViewModel(FitTimeDbContext db)
    {
        _db = db;
        Title = "Отчёты";
        _initialLoadTask = RunLoadAsync();
    }

    private async Task RunLoadAsync()
    {
        IsLoading = true;
        try
        {
            switch (SelectedTab)
            {
                case "Посещаемость":
                case "Загруженность":
                    await LoadAttendanceReportAsync();
                    break;
                case "Продажи":
                    await LoadSalesReportAsync();
                    break;
                case "Истекают":
                    await LoadExpiringReportAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Ошибка отчётов:\n{ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public override Task LoadAsync() => RunLoadAsync();

    private async Task LoadAttendanceReportAsync()
    {
        var attendances = await _db.Attendances
            .AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Class).ThenInclude(c => c.ClassType)
            .Include(a => a.Class).ThenInclude(c => c.Trainer)
            .Where(a => a.CheckedInAt >= DateFrom && a.CheckedInAt < DateTo.Date.AddDays(1))
            .ToListAsync();

        TotalAttendance = attendances.Count(a => a.Status == "Present");
        UniqueClients = attendances.Where(a => a.Status == "Present").Select(a => a.ClientId).Distinct().Count();
        var totalDays = (DateTo - DateFrom).Days;
        AvgPerDay = totalDays > 0 ? ((double)TotalAttendance / totalDays).ToString("F1") : "0";
        CancelledCount = attendances.Count(a => a.Status is "Cancelled" or "NoShow");

        // By day of week
        var byDay = attendances.Where(a => a.Status == "Present")
            .GroupBy(a => a.CheckedInAt.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        MondayCount = byDay.GetValueOrDefault(DayOfWeek.Monday, 0);
        TuesdayCount = byDay.GetValueOrDefault(DayOfWeek.Tuesday, 0);
        WednesdayCount = byDay.GetValueOrDefault(DayOfWeek.Wednesday, 0);
        ThursdayCount = byDay.GetValueOrDefault(DayOfWeek.Thursday, 0);
        FridayCount = byDay.GetValueOrDefault(DayOfWeek.Friday, 0);
        SaturdayCount = byDay.GetValueOrDefault(DayOfWeek.Saturday, 0);
        SundayCount = byDay.GetValueOrDefault(DayOfWeek.Sunday, 0);
        MaxBarHeight = Math.Max(1, new[] { MondayCount, TuesdayCount, WednesdayCount, ThursdayCount, FridayCount, SaturdayCount, SundayCount }.Max());

        // Top classes
        var classGroups = attendances
            .Where(a => a.Status == "Present")
            .GroupBy(a => new { a.Class.ClassType.Name, TrainerName = a.Class.Trainer.ShortName, a.Class.ClassType.Color })
            .Select(g =>
            {
                var totalEnrolled = g.Count();
                var maxCapacity = g.Sum(a => a.Class.MaxParticipants);
                var loadPercent = maxCapacity > 0 ? (double)totalEnrolled / maxCapacity * 100 : 0;
                var cancelled = attendances.Count(a => a.Class.ClassType.Name == g.Key.Name && a.Status is "Cancelled" or "NoShow");
                var totalForType = attendances.Count(a => a.Class.ClassType.Name == g.Key.Name);
                var cancelPercent = totalForType > 0 ? (double)cancelled / totalForType * 100 : 0;

                return new ReportTopClassDisplay
                {
                    ClassName = g.Key.Name,
                    TrainerName = g.Key.TrainerName,
                    AttendanceCount = totalEnrolled,
                    LoadPercent = loadPercent,
                    LoadBarWidth = loadPercent * 1.28, // scale to max 128px
                    LoadBarColor = GetClassColor(g.Key.Color),
                    CancelPercent = $"{cancelPercent:F0}%"
                };
            })
            .OrderByDescending(x => x.AttendanceCount)
            .Take(10)
            .ToList();

        TopClasses = new ObservableCollection<ReportTopClassDisplay>(classGroups);
    }

    private async Task LoadSalesReportAsync()
    {
        var memberships = await _db.Memberships
            .AsNoTracking()
            .Include(m => m.MembershipType)
            .Include(m => m.Client)
            .Where(m => m.CreatedAt >= DateFrom && m.CreatedAt < DateTo.Date.AddDays(1))
            .ToListAsync();

        SoldCount = memberships.Count;
        TotalRevenue = memberships.Sum(m => m.Price);
        AvgPrice = SoldCount > 0 ? TotalRevenue / SoldCount : 0;

        var byType = memberships
            .GroupBy(m => m.MembershipType.Name)
            .Select(g => new SalesSummaryItem
            {
                TypeName = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(m => m.Price)
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        SalesByType = new ObservableCollection<SalesSummaryItem>(byType);
    }

    private async Task LoadExpiringReportAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var in7Days = today.AddDays(7);

        var expiring = await _db.Memberships
            .AsNoTracking()
            .Include(m => m.Client)
            .Include(m => m.MembershipType)
            .Where(m => m.IsActive && m.EndDate >= today && m.EndDate <= in7Days)
            .OrderBy(m => m.EndDate)
            .ToListAsync();

        ExpiringCount = expiring.Count;

        var items = expiring.Select(m =>
        {
            var daysLeft = m.EndDate.DayNumber - today.DayNumber;
            return new ExpiringMembershipItem
            {
                ClientName = m.Client.FullName,
                TypeName = m.MembershipType.Name,
                EndDate = m.EndDate.ToString("dd.MM.yyyy"),
                DaysLeft = daysLeft,
                DaysLeftColor = daysLeft <= 3
                    ? new SolidColorBrush(Color.FromRgb(0xC0, 0x40, 0x40))
                    : new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48))
            };
        }).ToList();

        ExpiringMemberships = new ObservableCollection<ExpiringMembershipItem>(items);
    }

    private static SolidColorBrush GetClassColor(string hexColor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            return new SolidColorBrush(Color.FromRgb(
                (byte)(color.R / 3),
                (byte)(color.G / 3),
                (byte)(color.B / 3)));
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42));
        }
    }

    [RelayCommand]
    private void SetTab(string tab)
    {
        SelectedTab = tab;
    }

    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        await _initialLoadTask; // ensure initial load finished before reusing DbContext
        await RunLoadAsync();
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV файлы (*.csv)|*.csv",
            FileName = $"report_{SelectedTab}_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() != true) return;

        using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
        using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

        switch (SelectedTab)
        {
            case "Посещаемость":
            case "Загруженность":
                csv.WriteField("Занятие");
                csv.WriteField("Тренер");
                csv.WriteField("Посещений");
                csv.WriteField("Загруженность %");
                csv.WriteField("Отмен %");
                csv.NextRecord();
                foreach (var item in TopClasses)
                {
                    csv.WriteField(item.ClassName);
                    csv.WriteField(item.TrainerName);
                    csv.WriteField(item.AttendanceCount);
                    csv.WriteField($"{item.LoadPercent:F0}%");
                    csv.WriteField(item.CancelPercent);
                    csv.NextRecord();
                }
                break;

            case "Продажи":
                csv.WriteField("Тип абонемента");
                csv.WriteField("Продано");
                csv.WriteField("Выручка");
                csv.NextRecord();
                foreach (var item in SalesByType)
                {
                    csv.WriteField(item.TypeName);
                    csv.WriteField(item.Count);
                    csv.WriteField(item.Revenue);
                    csv.NextRecord();
                }
                break;

            case "Истекают":
                csv.WriteField("Клиент");
                csv.WriteField("Абонемент");
                csv.WriteField("Дата окончания");
                csv.WriteField("Дней осталось");
                csv.NextRecord();
                foreach (var item in ExpiringMemberships)
                {
                    csv.WriteField(item.ClientName);
                    csv.WriteField(item.TypeName);
                    csv.WriteField(item.EndDate);
                    csv.WriteField(item.DaysLeft);
                    csv.NextRecord();
                }
                break;
        }
    }

    partial void OnSelectedTabChanged(string value)
    {
        _ = ReloadAfterInitAsync();
    }

    private async Task ReloadAfterInitAsync()
    {
        await _initialLoadTask;
        await RunLoadAsync();
    }
}

public class SalesSummaryItem
{
    public string TypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class ExpiringMembershipItem
{
    public string ClientName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int DaysLeft { get; set; }
    public Brush DaysLeftColor { get; set; } = Brushes.White;
}

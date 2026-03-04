using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;

namespace FitTime.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;

    [ObservableProperty] private int _activeClientsCount;
    [ObservableProperty] private int _todayClassesCount;
    [ObservableProperty] private int _monthSalesCount;
    [ObservableProperty] private int _expiringMembershipsCount;

    // Weekly attendance chart (last 7 days)
    [ObservableProperty] private ObservableCollection<DayBarItem> _weeklyBars = new();

    // Today's classes
    [ObservableProperty] private ObservableCollection<TodayClassItem> _todayClasses = new();

    // Recent memberships
    [ObservableProperty] private ObservableCollection<RecentMembershipItem> _recentMemberships = new();

    public DashboardViewModel(FitTimeDbContext db)
    {
        _db = db;
        Title = "Панель управления";
        _ = LoadAsync();
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var weekFromNow = today.AddDays(7);
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);
            var monthStart = new DateTime(todayStart.Year, todayStart.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            ActiveClientsCount = await _db.Clients.CountAsync(c => c.IsActive);
            TodayClassesCount = await _db.Classes.CountAsync(c => c.StartTime >= todayStart && c.StartTime < todayEnd);
            MonthSalesCount = await _db.Memberships.CountAsync(m =>
                m.CreatedAt >= monthStart && m.CreatedAt < monthEnd);
            ExpiringMembershipsCount = await _db.Memberships.CountAsync(m =>
                m.IsActive && m.EndDate <= weekFromNow && m.EndDate >= today);

            await LoadWeeklyChartAsync();
            await LoadTodayClassesAsync();
            await LoadRecentMembershipsAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Dashboard error:\n{ex.Message}\n\n{ex.InnerException?.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadWeeklyChartAsync()
    {
        var dayNames = new[] { "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "ВС" };
        var bars = new List<DayBarItem>();

        // Last 7 days attendance — use range comparison instead of .Date
        for (int i = 6; i >= 0; i--)
        {
            var dayStart = DateTime.Today.AddDays(-i);
            var dayEnd = dayStart.AddDays(1);
            var count = await _db.Attendances
                .CountAsync(a => a.CheckedInAt >= dayStart && a.CheckedInAt < dayEnd && a.Status == "Present");

            bars.Add(new DayBarItem
            {
                DayLabel = dayNames[(int)dayStart.DayOfWeek == 0 ? 6 : (int)dayStart.DayOfWeek - 1],
                DateLabel = dayStart.ToString("dd.MM"),
                Count = count
            });
        }

        // Calculate bar heights proportionally
        var max = Math.Max(1, bars.Max(b => b.Count));
        foreach (var bar in bars)
        {
            bar.BarHeight = bar.Count > 0 ? Math.Max(4.0, (double)bar.Count / max * 140) : 0;
            bar.BarColor = bar.Count > 0
                ? new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48))
                : new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
        }

        WeeklyBars = new ObservableCollection<DayBarItem>(bars);
    }

    private async Task LoadTodayClassesAsync()
    {
        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1);
        var classes = await _db.Classes
            .Include(c => c.ClassType)
            .Include(c => c.Trainer)
            .Include(c => c.Enrollments)
            .Where(c => c.StartTime >= todayStart && c.StartTime < todayEnd)
            .OrderBy(c => c.StartTime)
            .Take(8)
            .ToListAsync();

        var items = classes.Select(c =>
        {
            var enrolled = c.Enrollments.Count(e => e.Status == "Enrolled");
            var (statusText, statusColor) = c.Status switch
            {
                "Cancelled" => ("ОТМЕНЕНО", new SolidColorBrush(Color.FromRgb(0xC0, 0x40, 0x40))),
                "Completed" => ("ЗАВЕРШЕНО", new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88))),
                _ when enrolled >= c.MaxParticipants => ("ЗАПОЛНЕНО", new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48))),
                _ => ("АКТИВНО", new SolidColorBrush(Color.FromRgb(0x7E, 0xD9, 0xA6)))
            };

            return new TodayClassItem
            {
                TimeText = $"{c.StartTime:HH:mm} — {c.EndTime:HH:mm}",
                ClassName = c.ClassType.Name,
                TrainerName = c.Trainer.ShortName,
                ParticipantsText = $"{enrolled}/{c.MaxParticipants}",
                StatusText = statusText,
                StatusColor = statusColor
            };
        }).ToList();

        TodayClasses = new ObservableCollection<TodayClassItem>(items);
    }

    private async Task LoadRecentMembershipsAsync()
    {
        var memberships = await _db.Memberships
            .Include(m => m.Client)
            .Include(m => m.MembershipType)
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .ToListAsync();

        var items = memberships.Select(m => new RecentMembershipItem
        {
            ClientName = m.Client.FullName,
            TypeName = m.MembershipType.Name,
            Price = $"{m.Price:N0} \u20bd",
            Date = m.CreatedAt.ToString("dd.MM.yyyy")
        }).ToList();

        RecentMemberships = new ObservableCollection<RecentMembershipItem>(items);
    }
}

public class DayBarItem
{
    public string DayLabel { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;
    public int Count { get; set; }
    public double BarHeight { get; set; }
    public Brush BarColor { get; set; } = Brushes.Gray;
}

public class TodayClassItem
{
    public string TimeText { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public string ParticipantsText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = Brushes.White;
}

public class RecentMembershipItem
{
    public string ClientName { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

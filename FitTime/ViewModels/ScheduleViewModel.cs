using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;
using FitTime.Services;

namespace FitTime.ViewModels;

public partial class ScheduleViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;
    private readonly INavigationService _nav;

    private const double HourHeight = 70.0;
    private const int GridStartHour = 7; // 07:00
    private const int GridEndHour = 22;  // 22:00

    [ObservableProperty] private ObservableCollection<ScheduleClassDisplay> _classes = new();
    [ObservableProperty] private ScheduleClassDisplay? _selectedClass;
    [ObservableProperty] private DateTime _weekStart;
    [ObservableProperty] private string _weekRangeText = string.Empty;
    [ObservableProperty] private bool _isWeekView = true;
    [ObservableProperty] private string _selectedTrainerFilter = "Все тренеры";
    [ObservableProperty] private string _selectedTypeFilter = "Все типы";
    [ObservableProperty] private ObservableCollection<string> _trainerFilters = new();
    [ObservableProperty] private ObservableCollection<string> _typeFilters = new();

    // Week view day headers
    [ObservableProperty] private string _day1Header = string.Empty;
    [ObservableProperty] private string _day2Header = string.Empty;
    [ObservableProperty] private string _day3Header = string.Empty;
    [ObservableProperty] private string _day4Header = string.Empty;
    [ObservableProperty] private string _day5Header = string.Empty;
    [ObservableProperty] private string _day6Header = string.Empty;
    [ObservableProperty] private string _day7Header = string.Empty;

    // Today highlight index (0=Mon..6=Sun, -1 if today not in this week)
    [ObservableProperty] private int _todayDayIndex = -1;

    // Per-day class collections for calendar grid
    [ObservableProperty] private ObservableCollection<ScheduleClassDisplay> _day0Classes = new();
    [ObservableProperty] private ObservableCollection<ScheduleClassDisplay> _day1Classes = new();
    [ObservableProperty] private ObservableCollection<ScheduleClassDisplay> _day2Classes = new();
    [ObservableProperty] private ObservableCollection<ScheduleClassDisplay> _day3Classes = new();
    [ObservableProperty] private ObservableCollection<ScheduleClassDisplay> _day4Classes = new();
    [ObservableProperty] private ObservableCollection<ScheduleClassDisplay> _day5Classes = new();
    [ObservableProperty] private ObservableCollection<ScheduleClassDisplay> _day6Classes = new();

    // Total grid height
    public double GridHeight => (GridEndHour - GridStartHour) * HourHeight;

    private static readonly string[] DayShortNames = { "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "ВС" };

    public ScheduleViewModel(FitTimeDbContext db, INavigationService nav)
    {
        _db = db;
        _nav = nav;
        Title = "Расписание";

        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        WeekStart = today.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));

        _ = LoadAsync();
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            UpdateWeekHeaders();
            await LoadFiltersAsync();

            var weekEnd = WeekStart.AddDays(7);

            var query = _db.Classes
                .Include(c => c.ClassType)
                .Include(c => c.Trainer)
                .Include(c => c.Enrollments)
                .Where(c => c.StartTime >= WeekStart && c.StartTime < weekEnd)
                .AsQueryable();

            var classes = await query
                .OrderBy(c => c.StartTime)
                .ToListAsync();

            // Apply filters
            if (SelectedTrainerFilter != "Все тренеры")
            {
                classes = classes.Where(c => c.Trainer.ShortName == SelectedTrainerFilter).ToList();
            }
            if (SelectedTypeFilter != "Все типы")
            {
                classes = classes.Where(c => c.ClassType.Name == SelectedTypeFilter).ToList();
            }

            var displays = classes.Select(c =>
            {
                var enrolled = c.Enrollments.Count(e => e.Status == "Enrolled");
                var isFull = enrolled >= c.MaxParticipants;
                var participantsText = $"{enrolled} / {c.MaxParticipants}";
                var participantsColor = isFull
                    ? new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48))
                    : Brushes.White;

                var (statusText, statusColor, statusBgColor) = c.Status switch
                {
                    "Cancelled" => ("ОТМЕНЕНО",
                        new SolidColorBrush(Color.FromRgb(0xC0, 0x40, 0x40)),
                        new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x2D))),
                    "Completed" => ("ЗАВЕРШЕНО",
                        new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A))),
                    _ when isFull => ("ЗАПОЛНЕНО",
                        new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48)),
                        new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x2A))),
                    _ => ("АКТИВНО",
                        new SolidColorBrush(Color.FromRgb(0x7E, 0xD9, 0xA6)),
                        new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42)))
                };

                var borderColor = GetClassTypeColor(c.ClassType.Color);

                var dayName = DayShortNames[(int)c.StartTime.DayOfWeek == 0 ? 6 : (int)c.StartTime.DayOfWeek - 1];
                var dateTimeText = $"{dayName}, {c.StartTime:dd.MM}  \u00b7  {c.StartTime:HH:mm} \u2013 {c.EndTime:HH:mm}";

                // Calendar grid positioning
                var dow = (int)c.StartTime.DayOfWeek;
                var dayCol = dow == 0 ? 6 : dow - 1; // Mon=0..Sun=6
                var topOffset = (c.StartTime.Hour - GridStartHour) * HourHeight
                                + c.StartTime.Minute / 60.0 * HourHeight;
                var durationMin = (c.EndTime - c.StartTime).TotalMinutes;
                var blockHeight = Math.Max(30, durationMin / 60.0 * HourHeight);

                return new ScheduleClassDisplay
                {
                    Id = c.Id,
                    ClassName = c.ClassType.Name,
                    Room = c.Room,
                    TrainerName = c.Trainer.ShortName,
                    DateTimeText = dateTimeText,
                    ParticipantsText = participantsText,
                    ParticipantsColor = participantsColor,
                    StatusText = statusText,
                    StatusColor = statusColor,
                    StatusBgColor = statusBgColor,
                    BorderColor = borderColor,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    Source = c,
                    DayColumn = dayCol,
                    TopOffset = topOffset,
                    BlockHeight = blockHeight
                };
            }).ToList();

            Classes = new ObservableCollection<ScheduleClassDisplay>(displays);

            // Split into per-day collections for calendar grid
            Day0Classes = new ObservableCollection<ScheduleClassDisplay>(displays.Where(d => d.DayColumn == 0));
            Day1Classes = new ObservableCollection<ScheduleClassDisplay>(displays.Where(d => d.DayColumn == 1));
            Day2Classes = new ObservableCollection<ScheduleClassDisplay>(displays.Where(d => d.DayColumn == 2));
            Day3Classes = new ObservableCollection<ScheduleClassDisplay>(displays.Where(d => d.DayColumn == 3));
            Day4Classes = new ObservableCollection<ScheduleClassDisplay>(displays.Where(d => d.DayColumn == 4));
            Day5Classes = new ObservableCollection<ScheduleClassDisplay>(displays.Where(d => d.DayColumn == 5));
            Day6Classes = new ObservableCollection<ScheduleClassDisplay>(displays.Where(d => d.DayColumn == 6));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Schedule error:\n{ex.Message}\n\n{ex.InnerException?.Message}", "Error");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadFiltersAsync()
    {
        var trainers = await _db.Users
            .Include(u => u.Role)
            .Where(u => u.Role.Name == "Trainer" && u.IsActive)
            .OrderBy(u => u.LastName)
            .Select(u => u.LastName + " " + u.FirstName.Substring(0, 1) + ".")
            .ToListAsync();

        var types = await _db.ClassTypes
            .Where(ct => ct.IsActive)
            .OrderBy(ct => ct.Name)
            .Select(ct => ct.Name)
            .ToListAsync();

        TrainerFilters = new ObservableCollection<string>(new[] { "Все тренеры" }.Concat(trainers));
        TypeFilters = new ObservableCollection<string>(new[] { "Все типы" }.Concat(types));
    }

    private void UpdateWeekHeaders()
    {
        var weekEnd = WeekStart.AddDays(6);
        var culture = new CultureInfo("ru-RU");
        WeekRangeText = $"{WeekStart:dd} {culture.DateTimeFormat.GetMonthName(WeekStart.Month)} \u2014 {weekEnd:dd} {culture.DateTimeFormat.GetMonthName(weekEnd.Month)} {weekEnd:yyyy}".ToUpper();

        TodayDayIndex = -1;
        for (int i = 0; i < 7; i++)
        {
            var day = WeekStart.AddDays(i);
            var header = $"{DayShortNames[i]}  {day.Day}";
            if (day.Date == DateTime.Today) TodayDayIndex = i;
            switch (i)
            {
                case 0: Day1Header = header; break;
                case 1: Day2Header = header; break;
                case 2: Day3Header = header; break;
                case 3: Day4Header = header; break;
                case 4: Day5Header = header; break;
                case 5: Day6Header = header; break;
                case 6: Day7Header = header; break;
            }
        }
    }

    private static SolidColorBrush GetClassTypeColor(string hexColor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            // Darken to ~40% for calendar blocks (visible but not blinding)
            return new SolidColorBrush(Color.FromRgb(
                (byte)(color.R * 0.4),
                (byte)(color.G * 0.4),
                (byte)(color.B * 0.4)));
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(0x2D, 0x40, 0x60));
        }
    }

    [RelayCommand]
    private void PreviousWeek()
    {
        WeekStart = WeekStart.AddDays(-7);
        _ = LoadAsync();
    }

    [RelayCommand]
    private void NextWeek()
    {
        WeekStart = WeekStart.AddDays(7);
        _ = LoadAsync();
    }

    [RelayCommand]
    private void SwitchToWeekView()
    {
        IsWeekView = true;
    }

    [RelayCommand]
    private void SwitchToListView()
    {
        IsWeekView = false;
    }

    [RelayCommand]
    private void AddClass()
    {
        _nav.NavigateTo<ClassDialogViewModel>(vm =>
        {
            vm.IsNew = true;
        });
    }

    [RelayCommand]
    private void EditClass()
    {
        if (SelectedClass == null) return;
        _nav.NavigateTo<ClassDialogViewModel>(vm => vm.LoadClass(SelectedClass.Id));
    }

    [RelayCommand]
    private void EditClassById(int id)
    {
        _nav.NavigateTo<ClassDialogViewModel>(vm => vm.LoadClass(id));
    }

    partial void OnSelectedTrainerFilterChanged(string value) => _ = LoadAsync();
    partial void OnSelectedTypeFilterChanged(string value) => _ = LoadAsync();
}

using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;
using FitTime.Services;

namespace FitTime.ViewModels;

public partial class ClassDialogViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;
    private readonly INavigationService _nav;
    private Class? _class;

    [ObservableProperty] private string _selectedTab = "Основное";
    [ObservableProperty] private bool _isNew;

    // Header
    [ObservableProperty] private string _className = string.Empty;
    [ObservableProperty] private string _classMeta = string.Empty;
    [ObservableProperty] private string _statusText = "АКТИВНО";
    [ObservableProperty] private Brush _statusColor = new SolidColorBrush(Color.FromRgb(0x7E, 0xD9, 0xA6));
    [ObservableProperty] private Brush _statusBgColor = new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42));
    [ObservableProperty] private Brush _accentColor = new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42));

    // Form fields
    [ObservableProperty] private ObservableCollection<ClassType> _classTypes = new();
    [ObservableProperty] private ClassType? _selectedClassType;
    [ObservableProperty] private ObservableCollection<User> _trainers = new();
    [ObservableProperty] private User? _selectedTrainer;
    [ObservableProperty] private DateTime _classDate = DateTime.Today;
    [ObservableProperty] private string _startTime = "09:00";
    [ObservableProperty] private string _endTime = "10:00";
    [ObservableProperty] private int _maxParticipants = 20;
    [ObservableProperty] private string _room = "Основной зал";
    [ObservableProperty] private string _status = "Scheduled";

    // Participants tab
    [ObservableProperty] private ObservableCollection<ClassParticipantDisplay> _participants = new();
    [ObservableProperty] private ClassParticipantDisplay? _selectedParticipant;
    [ObservableProperty] private string _participantsCountText = "0 из 0";
    [ObservableProperty] private ObservableCollection<Client> _availableClients = new();
    [ObservableProperty] private Client? _selectedNewClient;

    private Task _formDataTask = null!;

    public ClassDialogViewModel(FitTimeDbContext db, INavigationService nav)
    {
        _db = db;
        _nav = nav;
        Title = "Занятие";
        _formDataTask = LoadFormDataAsync();
    }

    private async Task LoadFormDataAsync()
    {
        var types = await _db.ClassTypes.Where(ct => ct.IsActive).OrderBy(ct => ct.Name).ToListAsync();
        ClassTypes = new ObservableCollection<ClassType>(types);

        var trainerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Trainer");
        if (trainerRole != null)
        {
            var trainers = await _db.Users
                .Where(u => u.RoleId == trainerRole.Id && u.IsActive)
                .OrderBy(u => u.LastName)
                .ToListAsync();
            Trainers = new ObservableCollection<User>(trainers);
        }
    }

    public void LoadClass(int classId)
    {
        _ = LoadClassAsync(classId);
    }

    private async Task LoadClassAsync(int classId)
    {
        IsLoading = true;
        try
        {
            await _formDataTask;

            _class = await _db.Classes
                .Include(c => c.ClassType)
                .Include(c => c.Trainer)
                .Include(c => c.Enrollments).ThenInclude(e => e.Client)
                .Include(c => c.Enrollments).ThenInclude(e => e.Membership).ThenInclude(m => m!.MembershipType)
                .Include(c => c.Attendances).ThenInclude(a => a.Client)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (_class == null) return;

            ClassName = _class.ClassType.Name;
            var dayNames = new[] { "ВС", "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ" };
            ClassMeta = $"{dayNames[(int)_class.StartTime.DayOfWeek]}, {_class.StartTime:dd MMMM yyyy}  \u00b7  {_class.StartTime:HH:mm} \u2014 {_class.EndTime:HH:mm}  \u00b7  {_class.Room}";

            SelectedClassType = ClassTypes.FirstOrDefault(ct => ct.Id == _class.ClassTypeId);
            SelectedTrainer = Trainers.FirstOrDefault(t => t.Id == _class.TrainerId);
            ClassDate = _class.StartTime.Date;
            StartTime = _class.StartTime.ToString("HH:mm");
            EndTime = _class.EndTime.ToString("HH:mm");
            MaxParticipants = _class.MaxParticipants;
            Room = _class.Room;
            Status = _class.Status;

            UpdateStatus();
            LoadParticipants();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Ошибка загрузки занятия:\n{ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateStatus()
    {
        var (st, sc, sbg) = Status switch
        {
            "Cancelled" => ("ОТМЕНЕНО",
                new SolidColorBrush(Color.FromRgb(0xC0, 0x40, 0x40)),
                new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x2D))),
            "Completed" => ("ЗАВЕРШЕНО",
                new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A))),
            _ => ("АКТИВНО",
                new SolidColorBrush(Color.FromRgb(0x7E, 0xD9, 0xA6)),
                new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42)))
        };
        StatusText = st;
        StatusColor = sc;
        StatusBgColor = sbg;
    }

    private void LoadParticipants()
    {
        if (_class == null) return;

        var enrolled = _class.Enrollments.Where(e => e.Status == "Enrolled").ToList();
        ParticipantsCountText = $"{enrolled.Count} из {_class.MaxParticipants}";

        var items = enrolled.Select(e =>
        {
            var attendance = _class.Attendances.FirstOrDefault(a => a.ClientId == e.ClientId);
            string stText;
            Brush stColor, stBg, bc;

            if (attendance == null)
            {
                stText = "НЕ ОТМЕЧЕН";
                stColor = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
                stBg = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
                bc = new SolidColorBrush(Color.FromRgb(0x2D, 0x40, 0x60));
            }
            else if (attendance.Status == "Present")
            {
                stText = "ОТМЕЧЕН";
                stColor = new SolidColorBrush(Color.FromRgb(0x7E, 0xD9, 0xA6));
                stBg = new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42));
                bc = new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42));
            }
            else
            {
                stText = "НЕ ЯВИЛСЯ";
                stColor = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
                stBg = new SolidColorBrush(Color.FromRgb(0x2A, 0x0A, 0x0A));
                bc = new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x2D));
            }

            var membershipInfo = e.Membership != null
                ? $"{e.Membership.MembershipType.Name} \u00b7 {(e.Membership.IsUnlimited ? "Безлимит" : $"{e.Membership.VisitsRemaining} посещений")} \u00b7 до {e.Membership.EndDate:dd.MM.yyyy}"
                : "Без абонемента";

            return new ClassParticipantDisplay
            {
                EnrollmentId = e.Id,
                ClientName = e.Client.FullName,
                MembershipInfo = membershipInfo,
                StatusText = stText,
                StatusColor = stColor,
                StatusBgColor = stBg,
                BorderColor = bc,
                EnrolledAt = e.EnrolledAt.ToString("dd.MM.yyyy HH:mm")
            };
        }).ToList();

        Participants = new ObservableCollection<ClassParticipantDisplay>(items);
        _ = LoadAvailableClientsAsync();
    }

    private async Task LoadAvailableClientsAsync()
    {
        if (_class == null) return;
        var enrolledClientIds = _class.Enrollments
            .Where(e => e.Status == "Enrolled")
            .Select(e => e.ClientId)
            .ToHashSet();

        var clients = await _db.Clients
            .Where(c => c.IsActive && !enrolledClientIds.Contains(c.Id))
            .OrderBy(c => c.LastName)
            .ToListAsync();
        AvailableClients = new ObservableCollection<Client>(clients);
        SelectedNewClient = null;
    }

    [RelayCommand]
    private async Task AddParticipantAsync()
    {
        if (_class == null || SelectedNewClient == null) return;

        try
        {
            var enrollment = new ClassEnrollment
            {
                ClassId = _class.Id,
                ClientId = SelectedNewClient.Id,
                EnrolledAt = DateTime.UtcNow,
                Status = "Enrolled"
            };
            _db.ClassEnrollments.Add(enrollment);
            await _db.SaveChangesAsync();
            await LoadClassAsync(_class.Id);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
        }
    }

    [RelayCommand]
    private async Task RemoveParticipantAsync()
    {
        if (_class == null || SelectedParticipant == null) return;

        var enrollment = await _db.ClassEnrollments.FindAsync(SelectedParticipant.EnrollmentId);
        if (enrollment == null) return;

        _db.ClassEnrollments.Remove(enrollment);
        await _db.SaveChangesAsync();
        await LoadClassAsync(_class.Id);
    }

    [RelayCommand]
    private async Task MarkPresentAsync()
    {
        if (_class == null || SelectedParticipant == null) return;

        var enrollment = await _db.ClassEnrollments.FindAsync(SelectedParticipant.EnrollmentId);
        if (enrollment == null) return;

        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.ClassId == _class.Id && a.ClientId == enrollment.ClientId);

        if (existing != null)
        {
            existing.Status = "Present";
        }
        else
        {
            _db.Attendances.Add(new Attendance
            {
                ClassId = _class.Id,
                ClientId = enrollment.ClientId,
                CheckedInAt = DateTime.UtcNow,
                Status = "Present"
            });
        }

        await _db.SaveChangesAsync();
        await LoadClassAsync(_class.Id);
    }

    [RelayCommand]
    private async Task MarkAbsentAsync()
    {
        if (_class == null || SelectedParticipant == null) return;

        var enrollment = await _db.ClassEnrollments.FindAsync(SelectedParticipant.EnrollmentId);
        if (enrollment == null) return;

        var existing = await _db.Attendances
            .FirstOrDefaultAsync(a => a.ClassId == _class.Id && a.ClientId == enrollment.ClientId);

        if (existing != null)
        {
            _db.Attendances.Remove(existing);
            await _db.SaveChangesAsync();
        }

        await LoadClassAsync(_class.Id);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedClassType == null || SelectedTrainer == null) return;

        if (!TimeSpan.TryParse(StartTime, out var start) || !TimeSpan.TryParse(EndTime, out var end))
            return;

        var startDt = ClassDate.Date + start;
        var endDt = ClassDate.Date + end;

        if (IsNew)
        {
            var newClass = new Class
            {
                ClassTypeId = SelectedClassType.Id,
                TrainerId = SelectedTrainer.Id,
                Room = Room,
                StartTime = startDt,
                EndTime = endDt,
                MaxParticipants = MaxParticipants,
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            };
            _db.Classes.Add(newClass);
        }
        else if (_class != null)
        {
            _class.ClassTypeId = SelectedClassType.Id;
            _class.TrainerId = SelectedTrainer.Id;
            _class.Room = Room;
            _class.StartTime = startDt;
            _class.EndTime = endDt;
            _class.MaxParticipants = MaxParticipants;
        }

        await _db.SaveChangesAsync();
        _nav.NavigateTo<ScheduleViewModel>();
    }

    [RelayCommand]
    private void Cancel()
    {
        _nav.NavigateTo<ScheduleViewModel>();
    }

    [RelayCommand]
    private void SetTab(string tab)
    {
        SelectedTab = tab;
    }
}

public class ClassParticipantDisplay
{
    public int EnrollmentId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string MembershipInfo { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = Brushes.White;
    public Brush StatusBgColor { get; set; } = Brushes.Transparent;
    public Brush BorderColor { get; set; } = Brushes.Transparent;
    public string EnrolledAt { get; set; } = string.Empty;
}

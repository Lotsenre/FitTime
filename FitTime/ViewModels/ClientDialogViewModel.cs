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

public partial class ClientDialogViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialog;
    private Client? _client;

    [ObservableProperty] private string _selectedTab = "Личные данные";
    [ObservableProperty] private bool _isNew;

    // Client header
    [ObservableProperty] private string _clientInitials = string.Empty;
    [ObservableProperty] private string _clientFullName = string.Empty;
    [ObservableProperty] private string _clientSubtitle = string.Empty;
    [ObservableProperty] private string _statusText = "АКТИВЕН";
    [ObservableProperty] private Brush _statusColor = new SolidColorBrush(Color.FromRgb(0x3D, 0xBE, 0x6A));
    [ObservableProperty] private Brush _statusBgColor = new SolidColorBrush(Color.FromRgb(0x0A, 0x20, 0x10));

    // Personal data fields
    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string? _patronymic;
    [ObservableProperty] private DateTime? _birthDate;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _notes;

    // Memberships tab
    [ObservableProperty] private ObservableCollection<ClientMembershipDisplay> _memberships = new();

    // Attendance tab
    [ObservableProperty] private ObservableCollection<ClientAttendanceDisplay> _attendances = new();

    public ClientDialogViewModel(FitTimeDbContext db, INavigationService nav, IDialogService dialog)
    {
        _db = db;
        _nav = nav;
        _dialog = dialog;
        Title = "Профиль клиента";
    }

    public void LoadClient(int? clientId)
    {
        if (clientId == null)
        {
            IsNew = true;
            ClientFullName = "Новый клиент";
            return;
        }
        _ = LoadClientAsync(clientId.Value);
    }

    private async Task LoadClientAsync(int clientId)
    {
        IsLoading = true;
        try
        {
            _client = await _db.Clients
                .Include(c => c.Memberships).ThenInclude(m => m.MembershipType)
                .Include(c => c.Attendances).ThenInclude(a => a.Class).ThenInclude(c => c.ClassType)
                .Include(c => c.Attendances).ThenInclude(a => a.Class).ThenInclude(c => c.Trainer)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (_client == null) return;

            FirstName = _client.FirstName;
            LastName = _client.LastName;
            Patronymic = _client.Patronymic;
            BirthDate = _client.BirthDate?.ToDateTime(TimeOnly.MinValue);
            Phone = _client.Phone;
            Email = _client.Email;
            Notes = _client.Notes;

            ClientInitials = _client.Initials;
            ClientFullName = _client.FullName;
            ClientSubtitle = $"ID {_client.Id:D4}  \u00b7  Зарегистрирован {_client.CreatedAt:dd.MM.yyyy}";

            if (_client.IsActive)
            {
                StatusText = "АКТИВЕН";
                StatusColor = new SolidColorBrush(Color.FromRgb(0x3D, 0xBE, 0x6A));
                StatusBgColor = new SolidColorBrush(Color.FromRgb(0x0A, 0x20, 0x10));
            }
            else
            {
                StatusText = "НЕАКТИВЕН";
                StatusColor = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
                StatusBgColor = new SolidColorBrush(Color.FromRgb(0x2A, 0x0A, 0x0A));
            }

            LoadMemberships();
            LoadAttendances();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadMemberships()
    {
        if (_client == null) return;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var items = _client.Memberships
            .OrderByDescending(m => m.EndDate)
            .Select(m =>
            {
                string statusText;
                Brush statusColor, statusBgColor, borderColor;

                if (m.IsActive && m.EndDate >= today)
                {
                    statusText = "АКТИВЕН";
                    statusColor = new SolidColorBrush(Color.FromRgb(0x3D, 0xBE, 0x6A));
                    statusBgColor = new SolidColorBrush(Color.FromRgb(0x0A, 0x20, 0x10));
                    borderColor = new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42));
                }
                else
                {
                    statusText = "ИСТЁК";
                    statusColor = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
                    statusBgColor = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
                    borderColor = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                }

                var typeName = m.MembershipType.Name;
                var visitInfo = m.IsUnlimited ? "Безлимит" : $"{m.MembershipType.VisitCount} посещений";

                return new ClientMembershipDisplay
                {
                    TypeName = $"{typeName} \u00b7 {visitInfo}",
                    StartDate = m.StartDate.ToString("dd.MM.yyyy"),
                    EndDate = m.EndDate.ToString("dd.MM.yyyy"),
                    VisitsText = m.IsUnlimited ? "\u221e" : $"{m.VisitsRemaining} / {m.MembershipType.VisitCount}",
                    VisitsColor = m.IsUnlimited ? new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48)) : Brushes.White,
                    StatusText = statusText,
                    StatusColor = statusColor,
                    StatusBgColor = statusBgColor,
                    BorderColor = borderColor
                };
            }).ToList();

        Memberships = new ObservableCollection<ClientMembershipDisplay>(items);
    }

    private void LoadAttendances()
    {
        if (_client == null) return;

        var items = _client.Attendances
            .OrderByDescending(a => a.CheckedInAt)
            .Take(50)
            .Select(a =>
            {
                var (st, sc, sbg, bc) = a.Status switch
                {
                    "Present" => ("ОТМЕЧЕН",
                        new SolidColorBrush(Color.FromRgb(0x7E, 0xD9, 0xA6)),
                        new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42)),
                        new SolidColorBrush(Color.FromRgb(0x2D, 0x5C, 0x42))),
                    "Cancelled" => ("ОТМЕНЕНО",
                        new SolidColorBrush(Color.FromRgb(0xC0, 0x40, 0x40)),
                        new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x2D)),
                        new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x2D))),
                    _ => ("НЕ ЯВИЛСЯ",
                        new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)),
                        new SolidColorBrush(Color.FromRgb(0x2A, 0x0A, 0x0A)),
                        new SolidColorBrush(Color.FromRgb(0x5C, 0x2D, 0x2D)))
                };

                return new ClientAttendanceDisplay
                {
                    ClassName = a.Class.ClassType.Name,
                    TrainerName = a.Class.Trainer.ShortName,
                    DateTimeText = a.CheckedInAt.ToString("dd.MM.yyyy  HH:mm"),
                    StatusText = st,
                    StatusColor = sc,
                    StatusBgColor = sbg,
                    BorderColor = bc
                };
            }).ToList();

        Attendances = new ObservableCollection<ClientAttendanceDisplay>(items);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(FirstName))
            return;

        if (IsNew)
        {
            _client = new Client
            {
                FirstName = FirstName,
                LastName = LastName,
                Patronymic = Patronymic,
                BirthDate = BirthDate != null ? DateOnly.FromDateTime(BirthDate.Value) : null,
                Phone = Phone,
                Email = Email,
                Notes = Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Clients.Add(_client);
        }
        else if (_client != null)
        {
            _client.FirstName = FirstName;
            _client.LastName = LastName;
            _client.Patronymic = Patronymic;
            _client.BirthDate = BirthDate != null ? DateOnly.FromDateTime(BirthDate.Value) : null;
            _client.Phone = Phone;
            _client.Email = Email;
            _client.Notes = Notes;
            _client.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _nav.NavigateTo<ClientsViewModel>();
    }

    [RelayCommand]
    private void Cancel()
    {
        _nav.NavigateTo<ClientsViewModel>();
    }

    [RelayCommand]
    private async Task AddMembershipAsync()
    {
        if (_client == null) return;
        var vm = new MembershipDialogViewModel(_db);
        vm.PresetClient(_client);
        var result = _dialog.ShowDialog<MembershipDialogWindow>(vm);
        if (result == true)
        {
            // Reload client data to refresh memberships list
            await LoadClientAsync(_client.Id);
        }
    }

    [RelayCommand]
    private void SetTab(string tab)
    {
        SelectedTab = tab;
    }
}

public class ClientMembershipDisplay
{
    public string TypeName { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string VisitsText { get; set; } = string.Empty;
    public Brush VisitsColor { get; set; } = Brushes.White;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = Brushes.White;
    public Brush StatusBgColor { get; set; } = Brushes.Transparent;
    public Brush BorderColor { get; set; } = Brushes.Transparent;
}

public class ClientAttendanceDisplay
{
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public string DateTimeText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = Brushes.White;
    public Brush StatusBgColor { get; set; } = Brushes.Transparent;
    public Brush BorderColor { get; set; } = Brushes.Transparent;
}

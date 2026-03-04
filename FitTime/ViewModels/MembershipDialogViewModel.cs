using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;

namespace FitTime.ViewModels;

public partial class MembershipDialogViewModel : ObservableObject
{
    private readonly FitTimeDbContext _db;

    [ObservableProperty] private ObservableCollection<Client> _clients = new();
    [ObservableProperty] private Client? _selectedClient;
    [ObservableProperty] private ObservableCollection<MembershipType> _membershipTypes = new();
    [ObservableProperty] private MembershipType? _selectedType;
    [ObservableProperty] private DateTime _startDate = DateTime.Today;
    [ObservableProperty] private string _endDateText = string.Empty;
    [ObservableProperty] private string _priceText = string.Empty;
    [ObservableProperty] private string _infoText = string.Empty;
    [ObservableProperty] private bool _dialogResult;

    public MembershipDialogViewModel(FitTimeDbContext db)
    {
        _db = db;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var clients = await _db.Clients
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName)
            .ToListAsync();
        Clients = new ObservableCollection<Client>(clients);

        var types = await _db.MembershipTypes
            .Where(t => !t.IsArchived)
            .OrderBy(t => t.Price)
            .ToListAsync();
        MembershipTypes = new ObservableCollection<MembershipType>(types);
    }

    public void PresetClient(Client client)
    {
        SelectedClient = client;
    }

    partial void OnSelectedTypeChanged(MembershipType? value)
    {
        if (value == null) return;
        var endDate = DateOnly.FromDateTime(StartDate).AddDays(value.DurationDays);
        EndDateText = endDate.ToString("dd.MM.yyyy");
        PriceText = $"{value.Price:N0} \u20bd";
        InfoText = value.IsUnlimited
            ? "Тип абонемента \u2014 Безлимит. Поле \u00abпосещений\u00bb не отображается."
            : $"Кол-во посещений: {value.VisitCount}";
    }

    partial void OnStartDateChanged(DateTime value)
    {
        if (SelectedType != null)
            OnSelectedTypeChanged(SelectedType);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedClient == null || SelectedType == null) return;

        try
        {
            var startDate = DateOnly.FromDateTime(StartDate);
            var membership = new Membership
            {
                ClientId = SelectedClient.Id,
                MembershipTypeId = SelectedType.Id,
                StartDate = startDate,
                EndDate = startDate.AddDays(SelectedType.DurationDays),
                IsUnlimited = SelectedType.IsUnlimited,
                VisitsRemaining = SelectedType.IsUnlimited ? 0 : SelectedType.VisitCount,
                IsActive = true,
                Price = SelectedType.Price,
                CreatedAt = DateTime.UtcNow
            };

            _db.Memberships.Add(membership);
            await _db.SaveChangesAsync();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Ошибка сохранения:\n{ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка");
        }
    }
}

using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;
using FitTime.Services;

namespace FitTime.ViewModels;

public partial class ClientsViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<ClientDisplay> _clients = new();
    [ObservableProperty] private ClientDisplay? _selectedClient;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedFilter = "Все";
    [ObservableProperty] private int _totalCount;

    public ClientsViewModel(FitTimeDbContext db, INavigationService nav, IDialogService dialog)
    {
        _db = db;
        _nav = nav;
        _dialog = dialog;
        Title = "Клиенты";
        _ = LoadAsync();
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var query = _db.Clients
                .Include(c => c.Memberships).ThenInclude(m => m.MembershipType)
                .Where(c => c.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                query = query.Where(c =>
                    c.LastName.ToLower().Contains(search) ||
                    c.FirstName.ToLower().Contains(search) ||
                    (c.Phone != null && c.Phone.Contains(search)) ||
                    (c.Email != null && c.Email.ToLower().Contains(search)));
            }

            var clients = await query.OrderBy(c => c.LastName).ToListAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Apply filter
            var filtered = SelectedFilter switch
            {
                "Активные" => clients.Where(c => c.Memberships.Any(m => m.IsActive && m.EndDate >= today)).ToList(),
                "Просроченные" => clients.Where(c => c.Memberships.Any(m => m.IsActive && m.EndDate < today)).ToList(),
                "Без аб." => clients.Where(c => !c.Memberships.Any(m => m.IsActive)).ToList(),
                _ => clients
            };

            var displays = filtered.Select(c =>
            {
                var activeMembership = c.Memberships
                    .Where(m => m.IsActive)
                    .OrderByDescending(m => m.EndDate)
                    .FirstOrDefault();

                string status;
                Brush color;

                if (activeMembership == null)
                {
                    status = "—";
                    color = Brushes.Gray;
                }
                else if (activeMembership.EndDate < today)
                {
                    var typeName = activeMembership.MembershipType.Name;
                    status = $"{typeName} · ПРОСРОЧЕН";
                    color = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
                }
                else
                {
                    var typeName = activeMembership.MembershipType.Name;
                    if (activeMembership.IsUnlimited)
                        status = $"{typeName} · Безлимит";
                    else
                        status = $"{typeName} · {activeMembership.VisitsRemaining} вис.";

                    var daysLeft = activeMembership.EndDate.DayNumber - today.DayNumber;
                    color = daysLeft <= 7
                        ? new SolidColorBrush(Color.FromRgb(0xF4, 0xB4, 0x48))
                        : Brushes.White;
                }

                return new ClientDisplay
                {
                    Id = c.Id,
                    FullName = $"{c.LastName} {c.FirstName} {c.Patronymic}".TrimEnd(),
                    Phone = c.Phone,
                    Email = c.Email,
                    MembershipStatus = status,
                    MembershipStatusColor = color,
                    CreatedAt = c.CreatedAt,
                    Source = c
                };
            }).ToList();

            TotalCount = clients.Count;
            Clients = new ObservableCollection<ClientDisplay>(displays);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SetFilter(string filter)
    {
        SelectedFilter = filter;
    }

    [RelayCommand]
    private void AddClient()
    {
        _nav.NavigateTo<ClientDialogViewModel>(vm => vm.LoadClient(null));
    }

    [RelayCommand]
    private void EditClient()
    {
        if (SelectedClient == null) return;
        _nav.NavigateTo<ClientDialogViewModel>(vm => vm.LoadClient(SelectedClient.Id));
    }

    [RelayCommand]
    private async Task DeactivateClientAsync()
    {
        if (SelectedClient == null) return;
        if (!_dialog.Confirm($"Деактивировать клиента {SelectedClient.FullName}?"))
            return;

        SelectedClient.Source.IsActive = false;
        SelectedClient.Source.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenProfile()
    {
        if (SelectedClient == null) return;
        _nav.NavigateTo<ClientDialogViewModel>(vm => vm.LoadClient(SelectedClient.Id));
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAsync();
    partial void OnSelectedFilterChanged(string value) => _ = LoadAsync();
}

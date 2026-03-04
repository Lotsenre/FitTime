using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitTime.Data;
using FitTime.Models;

namespace FitTime.ViewModels;

public partial class MembershipTypeDialogViewModel : ObservableObject
{
    private readonly FitTimeDbContext _db;
    private MembershipType? _existing;

    [ObservableProperty] private string _dialogTitle = "ДОБАВИТЬ ТИП АБОНЕМЕНТА";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _durationDays = 30;
    [ObservableProperty] private int _visitCount = 12;
    [ObservableProperty] private bool _isUnlimited;
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private bool _dialogResult;

    public MembershipTypeDialogViewModel(FitTimeDbContext db)
    {
        _db = db;
    }

    public void LoadExisting(MembershipType type)
    {
        _existing = type;
        DialogTitle = "РЕДАКТИРОВАТЬ ТИП АБОНЕМЕНТА";
        Name = type.Name;
        DurationDays = type.DurationDays;
        VisitCount = type.VisitCount;
        IsUnlimited = type.IsUnlimited;
        Price = type.Price;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;

        if (_existing != null)
        {
            _existing.Name = Name;
            _existing.DurationDays = DurationDays;
            _existing.VisitCount = IsUnlimited ? 0 : VisitCount;
            _existing.IsUnlimited = IsUnlimited;
            _existing.Price = Price;
        }
        else
        {
            var newType = new MembershipType
            {
                Name = Name,
                DurationDays = DurationDays,
                VisitCount = IsUnlimited ? 0 : VisitCount,
                IsUnlimited = IsUnlimited,
                Price = Price,
                CreatedAt = DateTime.UtcNow
            };
            _db.MembershipTypes.Add(newType);
        }

        await _db.SaveChangesAsync();
        DialogResult = true;
    }
}

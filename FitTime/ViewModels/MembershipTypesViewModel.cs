using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using FitTime.Data;
using FitTime.Models;
using FitTime.Services;
using FitTime.Views;

namespace FitTime.ViewModels;

public partial class MembershipTypesViewModel : BaseViewModel
{
    private readonly FitTimeDbContext _db;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<MembershipType> _membershipTypes = new();
    [ObservableProperty] private MembershipType? _selectedType;

    public MembershipTypesViewModel(FitTimeDbContext db, IDialogService dialog)
    {
        _db = db;
        _dialog = dialog;
        Title = "Абонементы";
        _ = LoadAsync();
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        var types = await _db.MembershipTypes
            .OrderBy(mt => mt.IsArchived)
            .ThenBy(mt => mt.Price)
            .ToListAsync();
        MembershipTypes = new ObservableCollection<MembershipType>(types);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task AddTypeAsync()
    {
        var vm = new MembershipTypeDialogViewModel(_db);
        var result = _dialog.ShowDialog<MembershipTypeDialogWindow>(vm);
        if (result == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task EditTypeAsync()
    {
        if (SelectedType == null) return;
        var vm = new MembershipTypeDialogViewModel(_db);
        vm.LoadExisting(SelectedType);
        var result = _dialog.ShowDialog<MembershipTypeDialogWindow>(vm);
        if (result == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task ArchiveTypeAsync()
    {
        if (SelectedType == null) return;
        SelectedType.IsArchived = !SelectedType.IsArchived;
        await _db.SaveChangesAsync();
        await LoadAsync();
    }
}

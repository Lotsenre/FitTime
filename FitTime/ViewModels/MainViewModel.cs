using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitTime.Services;
using FitTime.Views;

namespace FitTime.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private BaseViewModel _currentView = null!;
    [ObservableProperty] private bool _isMenuOpen = true;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _userRole = string.Empty;
    [ObservableProperty] private string _userInitials = string.Empty;
    [ObservableProperty] private int _selectedMenuIndex;
    [ObservableProperty] private int _activeNavIndex;

    // Role visibility
    [ObservableProperty] private bool _isAdminVisible;
    [ObservableProperty] private bool _isManagerOrAdminVisible;

    public MainViewModel(INavigationService navigation, ICurrentUserService currentUser)
    {
        _navigation = navigation;
        _currentUser = currentUser;

        var user = _currentUser.CurrentUser!;
        UserName = user.FullName;
        UserRole = user.Role.Name switch
        {
            "Admin" => "АДМИНИСТРАТОР",
            "Manager" => "МЕНЕДЖЕР",
            "Trainer" => "ТРЕНЕР",
            _ => user.Role.Name
        };
        UserInitials = string.Concat(
            user.LastName.Length > 0 ? user.LastName[0..1] : "",
            user.FirstName.Length > 0 ? user.FirstName[0..1] : "");

        IsAdminVisible = _currentUser.IsAdmin;
        IsManagerOrAdminVisible = _currentUser.IsAdmin || _currentUser.IsManager;

        _navigation.CurrentViewChanged += () => CurrentView = _navigation.CurrentView;

        // Navigate to dashboard by default
        NavigateToDashboard();
    }

    [RelayCommand] private void NavigateToDashboard() { _navigation.NavigateTo<DashboardViewModel>(); ActiveNavIndex = 0; }
    [RelayCommand] private void NavigateToClients() { _navigation.NavigateTo<ClientsViewModel>(); ActiveNavIndex = 1; }
    [RelayCommand] private void NavigateToMemberships() { _navigation.NavigateTo<MembershipTypesViewModel>(); ActiveNavIndex = 2; }
    [RelayCommand] private void NavigateToSchedule() { _navigation.NavigateTo<ScheduleViewModel>(); ActiveNavIndex = 3; }
    [RelayCommand] private void NavigateToTrainers() { _navigation.NavigateTo<TrainersViewModel>(); ActiveNavIndex = 4; }
    [RelayCommand] private void NavigateToAttendance() { _navigation.NavigateTo<AttendanceViewModel>(); ActiveNavIndex = 5; }
    [RelayCommand] private void NavigateToReports() { _navigation.NavigateTo<ReportsViewModel>(); ActiveNavIndex = 6; }
    [RelayCommand] private void NavigateToUsers() { _navigation.NavigateTo<UsersViewModel>(); ActiveNavIndex = 7; }

    [RelayCommand]
    private void Logout()
    {
        _currentUser.CurrentUser = null;

        var loginWindow = new LoginWindow
        {
            DataContext = App.Services.GetService(typeof(LoginViewModel))
        };
        Application.Current.MainWindow = loginWindow;
        loginWindow.Show();

        foreach (Window window in Application.Current.Windows)
        {
            if (window is MainWindow)
            {
                window.Close();
                break;
            }
        }
    }

    [RelayCommand]
    private void ToggleMenu() => IsMenuOpen = !IsMenuOpen;
}

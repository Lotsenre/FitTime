using FitTime.ViewModels;

namespace FitTime.Services;

public interface INavigationService
{
    BaseViewModel CurrentView { get; }
    void NavigateTo<T>() where T : BaseViewModel;
    void NavigateTo<T>(Action<T> configure) where T : BaseViewModel;
    event Action? CurrentViewChanged;
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private BaseViewModel _currentView = null!;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public BaseViewModel CurrentView
    {
        get => _currentView;
        private set
        {
            _currentView = value;
            CurrentViewChanged?.Invoke();
        }
    }

    public void NavigateTo<T>() where T : BaseViewModel
    {
        var viewModel = (T)_serviceProvider.GetService(typeof(T))!;
        CurrentView = viewModel;
    }

    public void NavigateTo<T>(Action<T> configure) where T : BaseViewModel
    {
        var viewModel = (T)_serviceProvider.GetService(typeof(T))!;
        configure(viewModel);
        CurrentView = viewModel;
    }

    public event Action? CurrentViewChanged;
}

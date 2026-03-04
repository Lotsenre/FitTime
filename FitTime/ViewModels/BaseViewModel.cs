using CommunityToolkit.Mvvm.ComponentModel;
using FitTime.Services;

namespace FitTime.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _title = string.Empty;

    public bool CanEdit
    {
        get
        {
            var svc = App.Services.GetService(typeof(ICurrentUserService)) as ICurrentUserService;
            return svc?.CanEdit ?? true;
        }
    }

    public virtual Task LoadAsync() => Task.CompletedTask;
}

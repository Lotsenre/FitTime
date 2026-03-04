using System.Windows;

namespace FitTime.Services;

public interface IDialogService
{
    bool? ShowDialog<T>(object? viewModel = null) where T : Window, new();
    bool Confirm(string message, string title = "Подтверждение");
}

public class DialogService : IDialogService
{
    public bool? ShowDialog<T>(object? viewModel = null) where T : Window, new()
    {
        var window = new T();
        if (viewModel != null)
            window.DataContext = viewModel;
        window.Owner = Application.Current.MainWindow;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        return window.ShowDialog();
    }

    public bool Confirm(string message, string title = "Подтверждение")
    {
        var result = MessageBox.Show(
            message, title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
}

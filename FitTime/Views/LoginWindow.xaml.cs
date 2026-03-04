using System.Windows;
using FitTime.ViewModels;

namespace FitTime.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // Pass password to ViewModel (PasswordBox doesn't support binding for security)
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }
}

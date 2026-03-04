using System.ComponentModel;
using System.Windows;

namespace FitTime.Views;

public partial class MembershipTypeDialogWindow : Window
{
    public MembershipTypeDialogWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is INotifyPropertyChanged vm)
            vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "DialogResult")
        {
            var prop = sender?.GetType().GetProperty("DialogResult");
            if (prop?.GetValue(sender) is true)
            {
                DialogResult = true;
                Close();
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}

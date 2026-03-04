using System.Windows.Controls;
using System.Windows.Input;
using FitTime.ViewModels;

namespace FitTime.Views;

public partial class ClientsView : UserControl
{
    public ClientsView() => InitializeComponent();

    private void ClientsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ClientsViewModel vm && vm.SelectedClient != null)
            vm.OpenProfileCommand.Execute(null);
    }
}

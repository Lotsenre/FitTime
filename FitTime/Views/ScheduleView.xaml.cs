using System.Windows.Controls;
using System.Windows.Input;
using FitTime.ViewModels;

namespace FitTime.Views;

public partial class ScheduleView : UserControl
{
    public ScheduleView() => InitializeComponent();

    private void ClassesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ScheduleViewModel vm && vm.SelectedClass != null)
            vm.EditClassCommand.Execute(null);
    }
}

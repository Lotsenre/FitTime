using System.Windows;

namespace FitTime.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }
}

using System.Windows.Controls;

namespace RestaurantOS.WPF.Views;

public partial class SettingsView : UserControl
{
    public SettingsView(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

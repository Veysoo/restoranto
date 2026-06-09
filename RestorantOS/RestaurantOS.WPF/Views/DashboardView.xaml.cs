using System.Windows.Controls;

namespace RestaurantOS.WPF.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

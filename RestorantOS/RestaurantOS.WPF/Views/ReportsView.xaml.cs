using System.Windows.Controls;

namespace RestaurantOS.WPF.Views;

public partial class ReportsView : UserControl
{
    public ReportsView(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

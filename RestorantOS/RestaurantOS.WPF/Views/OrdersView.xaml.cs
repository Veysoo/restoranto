using System.Windows.Controls;

namespace RestaurantOS.WPF.Views;

public partial class OrdersView : UserControl
{
    public OrdersView(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

using System.Windows.Controls;

namespace RestaurantOS.WPF.Views;

public partial class MenuView : UserControl
{
    public MenuView(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

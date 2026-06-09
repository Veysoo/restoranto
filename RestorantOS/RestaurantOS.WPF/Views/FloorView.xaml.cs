using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RestaurantOS.Application.DTOs.Floor;
using RestaurantOS.WPF.ViewModels;

namespace RestaurantOS.WPF.Views;

public partial class FloorView : UserControl
{
    private readonly FloorViewModel _vm;

    public FloorView(object viewModel)
    {
        InitializeComponent();
        _vm = (FloorViewModel)viewModel;
        DataContext = viewModel;
    }

    private async void TableCard_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.Tag is TableCardDto table)
                await _vm.SelectTableCommand.ExecuteAsync(table);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Masa açılırken hata:\n{ex.Message}", "RestaurantOS",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

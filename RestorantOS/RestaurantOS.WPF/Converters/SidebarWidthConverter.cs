using System.Globalization;
using System.Windows.Data;

namespace RestaurantOS.WPF.Converters;

public class SidebarWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? 220.0 : 72.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

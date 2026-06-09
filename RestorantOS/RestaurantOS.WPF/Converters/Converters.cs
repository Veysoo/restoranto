using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using RestaurantOS.Domain.Enums;

namespace RestaurantOS.WPF.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var inverted = value is bool b && !b;
        if (targetType == typeof(Visibility))
            return inverted ? Visibility.Visible : Visibility.Collapsed;
        return inverted;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
            return v != Visibility.Visible;
        return value is bool b && !b;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value == null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d) return d.ToString("₺#,##0.00", CultureInfo.GetCultureInfo("tr-TR"));
        if (value is double dbl) return dbl.ToString("₺#,##0.00", CultureInfo.GetCultureInfo("tr-TR"));
        return "₺0,00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TableStatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TableDisplayStatus status) return new SolidColorBrush(Color.FromRgb(71, 85, 105));
        var color = status switch
        {
            TableDisplayStatus.Empty => Color.FromRgb(71, 85, 105),
            TableDisplayStatus.Occupied => Color.FromRgb(245, 158, 11),
            TableDisplayStatus.Billed => Color.FromRgb(239, 68, 68),
            TableDisplayStatus.Paid => Color.FromRgb(16, 185, 129),
            _ => Color.FromRgb(71, 85, 105)
        };
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TableStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TableDisplayStatus status) return "BOŞTA";
        return status switch
        {
            TableDisplayStatus.Empty => "BOŞTA",
            TableDisplayStatus.Occupied => "DOLU",
            TableDisplayStatus.Billed => "HESAP İSTEDİ",
            TableDisplayStatus.Paid => "ÖDENDİ",
            _ => "BOŞTA"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class OrderStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not OrderItemStatus status) return "";
        return status switch
        {
            OrderItemStatus.Pending => "Bekliyor",
            OrderItemStatus.Preparing => "Hazırlanıyor",
            OrderItemStatus.Served => "Servis Edildi",
            OrderItemStatus.Cancelled => "İptal",
            _ => ""
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class DurationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime openedAt) return "";
        var span = DateTime.UtcNow - openedAt;
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}s {span.Minutes}dk";
        return $"{span.Minutes}dk {span.Seconds}sn";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StringMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? parameter ?? Binding.DoNothing : Binding.DoNothing;
}

public class ViewModelToViewConverter : IValueConverter
{
    private readonly Dictionary<Type, Type> _map = new()
    {
        [typeof(ViewModels.DashboardViewModel)] = typeof(Views.DashboardView),
        [typeof(ViewModels.FloorViewModel)] = typeof(Views.FloorView),
        [typeof(ViewModels.OrdersViewModel)] = typeof(Views.OrdersView),
        [typeof(ViewModels.MenuViewModel)] = typeof(Views.MenuView),
        [typeof(ViewModels.ReportsViewModel)] = typeof(Views.ReportsView),
        [typeof(ViewModels.SettingsViewModel)] = typeof(Views.SettingsView),
    };

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return null;
        var vmType = value.GetType();
        if (!_map.TryGetValue(vmType, out var viewType)) return null;
        return Activator.CreateInstance(viewType, value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

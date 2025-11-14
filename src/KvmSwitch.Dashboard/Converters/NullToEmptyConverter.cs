using System;
using System.Globalization;
using System.Windows.Data;

namespace KvmSwitch.Dashboard.Converters;

public class NullToEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? "Not connected";
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


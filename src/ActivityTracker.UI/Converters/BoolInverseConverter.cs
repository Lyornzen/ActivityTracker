using System.Globalization;
using System.Windows.Data;

namespace ActivityTracker.UI.Converters;

/// <summary>
/// Inverts a boolean value.
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class BoolInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

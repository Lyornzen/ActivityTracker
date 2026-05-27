using System.Globalization;
using System.Windows.Data;

namespace ActivityTracker.UI.Converters;

/// <summary>
/// Converts seconds (int) to a human-readable "Xh Ym" string.
/// </summary>
[ValueConversion(typeof(int), typeof(string))]
public class SecondsToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int seconds)
        {
            var hours = seconds / 3600;
            var minutes = (seconds % 3600) / 60;
            return $"{hours}h {minutes}m";
        }
        return "0h 0m";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

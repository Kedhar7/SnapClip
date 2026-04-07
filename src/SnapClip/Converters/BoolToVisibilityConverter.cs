using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SnapClip.Converters;

/// <summary>
/// Converts a boolean or integer value to a <see cref="Visibility"/> value.
/// Supports bool (true/false) and int (non-zero/zero) inputs.
/// Pass ConverterParameter="Invert" to invert the logic.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value switch
        {
            bool b => b,
            int i => i != 0,
            _ => false
        };
        bool invert = parameter is "Invert";

        if (invert) boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter is "Invert";
        bool isVisible = value is Visibility.Visible;

        return invert ? !isVisible : isVisible;
    }
}

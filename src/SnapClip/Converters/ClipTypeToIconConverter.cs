using System.Globalization;
using System.Windows.Data;
using SnapClip.Models;

namespace SnapClip.Converters;

/// <summary>
/// Converts a <see cref="ClipType"/> to its corresponding icon glyph string.
/// </summary>
public sealed class ClipTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ClipType clipType
            ? clipType switch
            {
                ClipType.Text => "\uE8C8",      // Document icon
                ClipType.Image => "\uE8B9",     // Photo icon
                ClipType.File => "\uE8E5",      // Folder icon
                ClipType.RichText => "\uE8A5",  // Rich text icon
                _ => "\uE8C8"
            }
            : "\uE8C8";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

using System.Globalization;
using System.Windows.Data;

namespace SnapClip.Converters;

/// <summary>
/// Converts a <see cref="DateTime"/> to a human-readable relative time string (e.g., "2m ago").
/// </summary>
public sealed class TimestampToRelativeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime timestamp)
            return string.Empty;

        return FormatRelativeTime(timestamp);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Formats a UTC timestamp as a relative time string.
    /// </summary>
    public static string FormatRelativeTime(DateTime utcTimestamp)
    {
        var elapsed = DateTime.UtcNow - utcTimestamp;

        if (elapsed.TotalSeconds < 5)
            return "just now";
        if (elapsed.TotalSeconds < 60)
            return $"{(int)elapsed.TotalSeconds}s ago";
        if (elapsed.TotalMinutes < 60)
            return $"{(int)elapsed.TotalMinutes}m ago";
        if (elapsed.TotalHours < 24)
            return $"{(int)elapsed.TotalHours}h ago";
        if (elapsed.TotalDays < 7)
            return $"{(int)elapsed.TotalDays}d ago";
        if (elapsed.TotalDays < 30)
            return $"{(int)(elapsed.TotalDays / 7)}w ago";

        return utcTimestamp.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }
}

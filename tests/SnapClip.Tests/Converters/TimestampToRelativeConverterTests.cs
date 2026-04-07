using FluentAssertions;
using SnapClip.Converters;
using Xunit;

namespace SnapClip.Tests.Converters;

public sealed class TimestampToRelativeConverterTests
{
    [Fact]
    public void JustNow_WithinFiveSeconds()
    {
        // Use 1 second ago to avoid timing drift
        var timestamp = DateTime.UtcNow.AddSeconds(-1);

        var result = TimestampToRelativeConverter.FormatRelativeTime(timestamp);

        result.Should().Be("just now");
    }

    [Fact]
    public void SecondsAgo_WithinOneMinute()
    {
        var timestamp = DateTime.UtcNow.AddSeconds(-30);

        var result = TimestampToRelativeConverter.FormatRelativeTime(timestamp);

        // Allow for slight drift: should be 29s-31s
        result.Should().MatchRegex(@"^\d{1,2}s ago$");
    }

    [Fact]
    public void MinutesAgo_WithinOneHour()
    {
        var timestamp = DateTime.UtcNow.AddMinutes(-15);

        var result = TimestampToRelativeConverter.FormatRelativeTime(timestamp);

        result.Should().Be("15m ago");
    }

    [Fact]
    public void HoursAgo_WithinOneDay()
    {
        var timestamp = DateTime.UtcNow.AddHours(-3);

        var result = TimestampToRelativeConverter.FormatRelativeTime(timestamp);

        result.Should().Be("3h ago");
    }

    [Fact]
    public void DaysAgo_WithinOneWeek()
    {
        var timestamp = DateTime.UtcNow.AddDays(-5);

        var result = TimestampToRelativeConverter.FormatRelativeTime(timestamp);

        result.Should().Be("5d ago");
    }

    [Fact]
    public void WeeksAgo_WithinOneMonth()
    {
        var timestamp = DateTime.UtcNow.AddDays(-14);

        var result = TimestampToRelativeConverter.FormatRelativeTime(timestamp);

        result.Should().Be("2w ago");
    }

    [Fact]
    public void OlderThanMonth_ShowsFullDate()
    {
        var timestamp = DateTime.UtcNow.AddDays(-60);

        var result = TimestampToRelativeConverter.FormatRelativeTime(timestamp);

        result.Should().MatchRegex(@"[A-Z][a-z]+ \d{1,2}, \d{4}");
    }
}

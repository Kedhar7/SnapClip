using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SnapClip.Data;
using SnapClip.Services;
using Xunit;

namespace SnapClip.Tests.Services;

public sealed class TelemetryServiceTests : IAsyncDisposable
{
    private readonly IDbContextFactory<SnapClipDbContext> _factory;
    private readonly TelemetryService _sut;

    public TelemetryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SnapClipDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _sut = new TelemetryService(_factory);

        using var context = new SnapClipDbContext(options);
        context.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task LogEvent_PersistsToDatabase()
    {
        await _sut.LogEventAsync("test_event", new { key = "value" });

        var events = await _sut.GetRecentEventsAsync(10);
        events.Should().ContainSingle();
        events[0].EventName.Should().Be("test_event");
        events[0].EventData.Should().Contain("value");
    }

    [Fact]
    public async Task GetEventsByDateRange_FiltersCorrectly()
    {
        await _sut.LogEventAsync("event_in_range");

        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow.AddHours(1);
        var events = await _sut.GetEventsByDateRangeAsync(from, to);

        events.Should().ContainSingle();
        events[0].EventName.Should().Be("event_in_range");
    }

    [Fact]
    public async Task GetEventsByDateRange_ExcludesOutOfRange()
    {
        await _sut.LogEventAsync("current_event");

        var from = DateTime.UtcNow.AddDays(-10);
        var to = DateTime.UtcNow.AddDays(-5);
        var events = await _sut.GetEventsByDateRangeAsync(from, to);

        events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTodayCount_ReturnsCorrectCount()
    {
        await _sut.LogEventAsync("clip_captured");
        await _sut.LogEventAsync("clip_captured");
        await _sut.LogEventAsync("clip_pasted");

        var count = await _sut.GetTodayCountAsync("clip_captured");

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetUsageByHour_ReturnsCorrectDistribution()
    {
        await _sut.LogEventAsync("event1");
        await _sut.LogEventAsync("event2");
        await _sut.LogEventAsync("event3");

        var distribution = await _sut.GetUsageByHourAsync();

        distribution.Should().NotBeEmpty();
        int currentHour = DateTime.UtcNow.Hour;
        distribution.Should().ContainKey(currentHour);
        distribution[currentHour].Should().Be(3);
    }

    [Fact]
    public async Task GetTotalEventCount_ReturnsCorrectTotal()
    {
        await _sut.LogEventAsync("e1");
        await _sut.LogEventAsync("e2");

        var total = await _sut.GetTotalEventCountAsync();

        total.Should().Be(2);
    }

    [Fact]
    public async Task LogEvent_WithNullData_StoresNullEventData()
    {
        await _sut.LogEventAsync("simple_event");

        var events = await _sut.GetRecentEventsAsync(1);
        events[0].EventData.Should().BeNull();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<SnapClipDbContext>
    {
        private readonly DbContextOptions<SnapClipDbContext> _options;

        public TestDbContextFactory(DbContextOptions<SnapClipDbContext> options)
        {
            _options = options;
        }

        public SnapClipDbContext CreateDbContext()
        {
            return new SnapClipDbContext(_options);
        }
    }
}

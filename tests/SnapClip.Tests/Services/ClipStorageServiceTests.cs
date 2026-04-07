using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SnapClip.Data;
using SnapClip.Models;
using SnapClip.Services;
using Xunit;

namespace SnapClip.Tests.Services;

public sealed class ClipStorageServiceTests : IAsyncDisposable
{
    private readonly IDbContextFactory<SnapClipDbContext> _factory;
    private readonly ClipStorageService _sut;

    public ClipStorageServiceTests()
    {
        var options = new DbContextOptionsBuilder<SnapClipDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _factory = new TestDbContextFactory(options);
        _sut = new ClipStorageService(_factory);

        // Ensure database is created
        using var context = new SnapClipDbContext(options);
        context.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task SaveClip_PersistsToDatabase()
    {
        var clip = CreateClip("Test content");

        var saved = await _sut.SaveClipAsync(clip);

        saved.Id.Should().BeGreaterThan(0);
        var retrieved = await _sut.GetClipByIdAsync(saved.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Content.Should().Be("Test content");
    }

    [Fact]
    public async Task GetClips_ReturnsPinnedFirst()
    {
        await _sut.SaveClipAsync(CreateClip("First"));
        var pinned = await _sut.SaveClipAsync(CreateClip("Pinned", isPinned: true));
        await _sut.SaveClipAsync(CreateClip("Last"));

        var clips = await _sut.GetClipsAsync();

        clips[0].IsPinned.Should().BeTrue();
        clips[0].Content.Should().Be("Pinned");
    }

    [Fact]
    public async Task DeleteClip_RemovesFromDatabase()
    {
        var clip = await _sut.SaveClipAsync(CreateClip("To delete"));

        await _sut.DeleteClipAsync(clip.Id);

        var retrieved = await _sut.GetClipByIdAsync(clip.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task MaxHistoryEnforced_DeletesOldestNonPinned()
    {
        _sut.MaxHistorySize = 3;

        await _sut.SaveClipAsync(CreateClip("Old 1", capturedAt: DateTime.UtcNow.AddMinutes(-4)));
        await _sut.SaveClipAsync(CreateClip("Old 2", capturedAt: DateTime.UtcNow.AddMinutes(-3)));
        await _sut.SaveClipAsync(CreateClip("Recent 1", capturedAt: DateTime.UtcNow.AddMinutes(-2)));
        await _sut.SaveClipAsync(CreateClip("New", capturedAt: DateTime.UtcNow.AddMinutes(-1)));

        var clips = await _sut.GetClipsAsync();
        clips.Should().HaveCountLessOrEqualTo(3);
        clips.Select(c => c.Content).Should().Contain("New");
        clips.Select(c => c.Content).Should().Contain("Recent 1");
        clips.Select(c => c.Content).Should().NotContain("Old 1");
    }

    [Fact]
    public async Task MaxHistoryEnforced_PinnedClipsPreserved()
    {
        _sut.MaxHistorySize = 2;

        await _sut.SaveClipAsync(CreateClip("Pinned old", capturedAt: DateTime.UtcNow.AddMinutes(-3), isPinned: true));
        await _sut.SaveClipAsync(CreateClip("Regular 1", capturedAt: DateTime.UtcNow.AddMinutes(-2)));
        await _sut.SaveClipAsync(CreateClip("Regular 2", capturedAt: DateTime.UtcNow.AddMinutes(-1)));

        var clips = await _sut.GetClipsAsync();
        clips.Select(c => c.Content).Should().Contain("Pinned old");
    }

    [Fact]
    public async Task DuplicateClip_NotStored()
    {
        await _sut.SaveClipAsync(CreateClip("Duplicate content"));
        await _sut.SaveClipAsync(CreateClip("Duplicate content"));

        var count = await _sut.GetClipCountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task TogglePin_ChangesPinnedState()
    {
        var clip = await _sut.SaveClipAsync(CreateClip("Pinnable"));
        clip.IsPinned.Should().BeFalse();

        await _sut.TogglePinAsync(clip.Id);

        var updated = await _sut.GetClipByIdAsync(clip.Id);
        updated!.IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task RecordPaste_IncrementsPasteCount()
    {
        var clip = await _sut.SaveClipAsync(CreateClip("Pasteable"));

        await _sut.RecordPasteAsync(clip.Id);
        await _sut.RecordPasteAsync(clip.Id);

        var updated = await _sut.GetClipByIdAsync(clip.Id);
        updated!.PasteCount.Should().Be(2);
        updated.LastPastedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ClearHistory_RemovesNonPinnedOnly()
    {
        await _sut.SaveClipAsync(CreateClip("Pinned", isPinned: true));
        await _sut.SaveClipAsync(CreateClip("Regular 1"));
        await _sut.SaveClipAsync(CreateClip("Regular 2"));

        await _sut.ClearHistoryAsync();

        var count = await _sut.GetClipCountAsync();
        count.Should().Be(1); // Only pinned remains

        var clips = await _sut.GetPinnedClipsAsync();
        clips.Should().ContainSingle();
        clips[0].Content.Should().Be("Pinned");
    }

    [Fact]
    public async Task GetTotalPasteCount_ReturnsSum()
    {
        var clip1 = await _sut.SaveClipAsync(CreateClip("Clip 1"));
        var clip2 = await _sut.SaveClipAsync(CreateClip("Clip 2"));

        await _sut.RecordPasteAsync(clip1.Id);
        await _sut.RecordPasteAsync(clip1.Id);
        await _sut.RecordPasteAsync(clip2.Id);

        var total = await _sut.GetTotalPasteCountAsync();
        total.Should().Be(3);
    }

    private static ClipItem CreateClip(string content, bool isPinned = false, DateTime? capturedAt = null)
    {
        return new ClipItem
        {
            Content = content,
            Type = ClipType.Text,
            CapturedAt = capturedAt ?? DateTime.UtcNow,
            IsPinned = isPinned
        };
    }

    /// <summary>
    /// Simple factory wrapper for in-memory database testing.
    /// </summary>
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

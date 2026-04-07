using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SnapClip.Data;
using SnapClip.Models;
using SnapClip.Services;
using SnapClip.ViewModels;
using Xunit;

namespace SnapClip.Tests.ViewModels;

public sealed class MainViewModelTests : IDisposable
{
    private readonly SearchService _searchService;
    private readonly DbContextOptions<SnapClipDbContext> _options;

    public MainViewModelTests()
    {
        _options = new DbContextOptionsBuilder<SnapClipDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new SnapClipDbContext(_options);
        context.Database.EnsureCreated();

        _searchService = new SearchService();
    }

    public void Dispose()
    {
        using var context = new SnapClipDbContext(_options);
        context.Database.EnsureDeleted();
    }

    [Fact]
    public void SearchTextChanged_FiltersClips()
    {
        var clips = new[]
        {
            new ClipItem { Id = 1, Content = "hello world", Type = ClipType.Text, CapturedAt = DateTime.UtcNow },
            new ClipItem { Id = 2, Content = "goodbye world", Type = ClipType.Text, CapturedAt = DateTime.UtcNow },
            new ClipItem { Id = 3, Content = "unrelated content", Type = ClipType.Text, CapturedAt = DateTime.UtcNow }
        };
        _searchService.RebuildIndex(clips);

        var results = _searchService.Search("world");

        results.Should().HaveCount(2);
        results.Select(r => r.Content).Should().OnlyContain(c => c.Contains("world"));
    }

    [Fact]
    public void NewClipCaptured_AddedToSearchIndex()
    {
        _searchService.RebuildIndex(Array.Empty<ClipItem>());

        var newClip = new ClipItem
        {
            Id = 1,
            Content = "brand new clip",
            Type = ClipType.Text,
            CapturedAt = DateTime.UtcNow
        };
        _searchService.AddToIndex(newClip);

        var results = _searchService.Search("brand new");
        results.Should().ContainSingle();
        results[0].Content.Should().Be("brand new clip");
    }

    [Fact]
    public void DeleteClip_RemovedFromSearchIndex()
    {
        var clip = new ClipItem { Id = 42, Content = "deleteable", Type = ClipType.Text, CapturedAt = DateTime.UtcNow };
        _searchService.RebuildIndex(new[] { clip });

        _searchService.RemoveFromIndex(42);

        var results = _searchService.Search("deleteable");
        results.Should().BeEmpty();
    }

    [Fact]
    public void ClipItemViewModel_Preview_TruncatesLongText()
    {
        var clip = new ClipItem
        {
            Content = new string('A', 200),
            Type = ClipType.Text,
            CapturedAt = DateTime.UtcNow
        };
        var vm = new ClipItemViewModel(clip);

        vm.Preview.Length.Should().BeLessOrEqualTo(103); // 100 chars + "..."
        vm.Preview.Should().EndWith("...");
    }

    [Fact]
    public void ClipItemViewModel_Preview_ShortTextNotTruncated()
    {
        var clip = new ClipItem
        {
            Content = "Short text",
            Type = ClipType.Text,
            CapturedAt = DateTime.UtcNow
        };
        var vm = new ClipItemViewModel(clip);

        vm.Preview.Should().Be("Short text");
    }

    [Fact]
    public void ClipItemViewModel_IsPinned_ToggleNotifiesChange()
    {
        var clip = new ClipItem { Content = "test", Type = ClipType.Text, CapturedAt = DateTime.UtcNow };
        var vm = new ClipItemViewModel(clip);
        bool changed = false;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ClipItemViewModel.IsPinned))
                changed = true;
        };

        vm.IsPinned = true;

        changed.Should().BeTrue();
        vm.IsPinned.Should().BeTrue();
        vm.Model.IsPinned.Should().BeTrue(); // Verify model is updated too
    }

    [Fact]
    public void ClipItemViewModel_IsFavorite_ToggleNotifiesChange()
    {
        var clip = new ClipItem { Content = "test", Type = ClipType.Text, CapturedAt = DateTime.UtcNow };
        var vm = new ClipItemViewModel(clip);
        bool changed = false;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ClipItemViewModel.IsFavorite))
                changed = true;
        };

        vm.IsFavorite = true;

        changed.Should().BeTrue();
        vm.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public void ClipItemViewModel_FileClip_FormatsPathsCorrectly()
    {
        var clip = new ClipItem
        {
            Content = @"C:\Users\test\report.pdf" + Environment.NewLine + @"C:\Users\test\data.csv",
            Type = ClipType.File,
            CapturedAt = DateTime.UtcNow
        };
        var vm = new ClipItemViewModel(clip);

        vm.Preview.Should().Contain("report.pdf");
        vm.Preview.Should().Contain("data.csv");
    }

    [Fact]
    public void ClipItemViewModel_RelativeTime_ReturnsFormattedString()
    {
        var clip = new ClipItem
        {
            Content = "test",
            Type = ClipType.Text,
            CapturedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var vm = new ClipItemViewModel(clip);

        vm.RelativeTime.Should().Be("5m ago");
    }
}

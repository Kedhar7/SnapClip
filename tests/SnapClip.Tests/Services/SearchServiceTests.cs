using FluentAssertions;
using SnapClip.Models;
using SnapClip.Services;
using Xunit;

namespace SnapClip.Tests.Services;

public sealed class SearchServiceTests
{
    private readonly SearchService _sut = new();

    private static ClipItem CreateClip(string content, DateTime? capturedAt = null, bool isPinned = false)
    {
        return new ClipItem
        {
            Id = Random.Shared.Next(1, 100000),
            Content = content,
            Type = ClipType.Text,
            CapturedAt = capturedAt ?? DateTime.UtcNow,
            IsPinned = isPinned
        };
    }

    [Fact]
    public void SearchByExactMatch_ReturnsTopResult()
    {
        var clips = new[]
        {
            CreateClip("hello world"),
            CreateClip("hello there"),
            CreateClip("world hello")
        };
        _sut.RebuildIndex(clips);

        var results = _sut.Search("hello world");

        results.Should().NotBeEmpty();
        results[0].Content.Should().Be("hello world");
    }

    [Fact]
    public void SearchBySubstring_ReturnsResults()
    {
        var clips = new[]
        {
            CreateClip("the quick brown fox"),
            CreateClip("lazy dog"),
            CreateClip("fox trot dance")
        };
        _sut.RebuildIndex(clips);

        var results = _sut.Search("fox");

        results.Should().HaveCount(2);
        results.Select(r => r.Content).Should().Contain("the quick brown fox");
        results.Select(r => r.Content).Should().Contain("fox trot dance");
    }

    [Fact]
    public void SearchIsCaseInsensitive()
    {
        var clips = new[] { CreateClip("Hello World"), CreateClip("goodbye") };
        _sut.RebuildIndex(clips);

        var results = _sut.Search("hello world");

        results.Should().ContainSingle();
        results[0].Content.Should().Be("Hello World");
    }

    [Fact]
    public void SearchWithNoResults_ReturnsEmpty()
    {
        var clips = new[] { CreateClip("apple"), CreateClip("banana") };
        _sut.RebuildIndex(clips);

        var results = _sut.Search("cherry");

        results.Should().BeEmpty();
    }

    [Fact]
    public void SearchPerformance_Under50ms_With1000Clips()
    {
        var clips = Enumerable.Range(0, 1000)
            .Select(i => CreateClip($"Clip content number {i} with some extra text to search through"))
            .ToList();
        _sut.RebuildIndex(clips);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = _sut.Search("number 500");
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(50);
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void SearchRanking_ExactMatchBeforeSubstring()
    {
        var clips = new[]
        {
            CreateClip("contains fox in middle"),
            CreateClip("fox starts this sentence"),
            CreateClip("fox")
        };
        _sut.RebuildIndex(clips);

        var results = _sut.Search("fox");

        results.Should().HaveCount(3);
        results[0].Content.Should().Be("fox"); // Exact match first (score 100)
        results[1].Content.Should().Be("fox starts this sentence"); // Starts-with second (score 75)
        results[2].Content.Should().Be("contains fox in middle"); // Contains last (score 50)
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAllClips()
    {
        var clips = new[] { CreateClip("one"), CreateClip("two"), CreateClip("three") };
        _sut.RebuildIndex(clips);

        var results = _sut.Search("");

        results.Should().HaveCount(3);
    }

    [Fact]
    public void Search_WhitespaceQuery_ReturnsAllClips()
    {
        var clips = new[] { CreateClip("one"), CreateClip("two") };
        _sut.RebuildIndex(clips);

        var results = _sut.Search("   ");

        results.Should().HaveCount(2);
    }

    [Fact]
    public void AddToIndex_NewClipIsSearchable()
    {
        _sut.RebuildIndex(Array.Empty<ClipItem>());

        var clip = CreateClip("new clip content");
        _sut.AddToIndex(clip);

        var results = _sut.Search("new clip");
        results.Should().ContainSingle();
    }

    [Fact]
    public void RemoveFromIndex_ClipNoLongerSearchable()
    {
        var clip = CreateClip("removable clip");
        _sut.RebuildIndex(new[] { clip });

        _sut.RemoveFromIndex(clip.Id);

        var results = _sut.Search("removable");
        results.Should().BeEmpty();
    }

    [Fact]
    public void Search_PinnedClips_BoostedInResults()
    {
        var pinnedClip = CreateClip("fox content", isPinned: true);
        var normalClip = CreateClip("fox content", isPinned: false);
        _sut.RebuildIndex(new[] { normalClip, pinnedClip });

        var results = _sut.Search("fox");

        results.Should().HaveCount(2);
        results[0].IsPinned.Should().BeTrue();
    }
}

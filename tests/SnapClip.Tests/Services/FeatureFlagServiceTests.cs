using FluentAssertions;
using SnapClip.Services;
using Xunit;

namespace SnapClip.Tests.Services;

public sealed class FeatureFlagServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FeatureFlagService _sut;

    public FeatureFlagServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SnapClipTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _sut = new FeatureFlagService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void IsEnabled_ReturnsTrueForEnabledFlag()
    {
        // SmartCategorization is enabled by default
        _sut.IsEnabled(FeatureFlagService.Flags.SmartCategorization).Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_ReturnsFalseForDisabledFlag()
    {
        // ImageOcr is disabled by default
        _sut.IsEnabled(FeatureFlagService.Flags.ImageOcr).Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_ReturnsFalseForUnknownFlag()
    {
        _sut.IsEnabled("NonExistentFeature").Should().BeFalse();
    }

    [Fact]
    public void SetFlag_PersistsToFile()
    {
        _sut.SetFlag(FeatureFlagService.Flags.ImageOcr, true);

        // Create a new instance to verify persistence
        var newService = new FeatureFlagService(_tempDir);
        newService.IsEnabled(FeatureFlagService.Flags.ImageOcr).Should().BeTrue();
    }

    [Fact]
    public void GetAllFlags_ReturnsCompleteList()
    {
        var flags = _sut.GetAllFlags();

        flags.Should().ContainKey(FeatureFlagService.Flags.SmartCategorization);
        flags.Should().ContainKey(FeatureFlagService.Flags.ImageOcr);
        flags.Should().ContainKey(FeatureFlagService.Flags.SensitiveContentDetection);
        flags.Should().ContainKey(FeatureFlagService.Flags.ClipMerge);
        flags.Should().ContainKey(FeatureFlagService.Flags.SoundEffects);
        flags.Should().HaveCount(5);
    }

    [Fact]
    public void SetFlag_ToggleExistingFlag()
    {
        _sut.IsEnabled(FeatureFlagService.Flags.SmartCategorization).Should().BeTrue();

        _sut.SetFlag(FeatureFlagService.Flags.SmartCategorization, false);

        _sut.IsEnabled(FeatureFlagService.Flags.SmartCategorization).Should().BeFalse();
    }

    [Fact]
    public void SetFlag_AddNewFlag()
    {
        _sut.SetFlag("CustomFeature", true);

        _sut.IsEnabled("CustomFeature").Should().BeTrue();
        _sut.GetAllFlags().Should().ContainKey("CustomFeature");
    }
}

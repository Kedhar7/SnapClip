using FluentAssertions;
using SnapClip.Services;
using Xunit;

namespace SnapClip.Tests.ViewModels;

public sealed class SettingsViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FeatureFlagService _featureFlagService;

    public SettingsViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SnapClipTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _featureFlagService = new FeatureFlagService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void FeatureFlagService_DefaultFlags_AreCorrect()
    {
        var flags = _featureFlagService.GetAllFlags();

        flags[FeatureFlagService.Flags.SmartCategorization].Should().BeTrue();
        flags[FeatureFlagService.Flags.ImageOcr].Should().BeFalse();
        flags[FeatureFlagService.Flags.SensitiveContentDetection].Should().BeTrue();
        flags[FeatureFlagService.Flags.ClipMerge].Should().BeFalse();
        flags[FeatureFlagService.Flags.SoundEffects].Should().BeFalse();
    }

    [Fact]
    public void FeatureFlagService_SetFlag_PersistsAcrossInstances()
    {
        _featureFlagService.SetFlag(FeatureFlagService.Flags.ClipMerge, true);

        var newInstance = new FeatureFlagService(_tempDir);
        newInstance.IsEnabled(FeatureFlagService.Flags.ClipMerge).Should().BeTrue();
    }
}

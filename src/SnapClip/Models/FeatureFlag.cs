namespace SnapClip.Models;

/// <summary>
/// Represents a feature flag for toggling experimental features.
/// </summary>
public sealed class FeatureFlag
{
    public string Name { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public string? Description { get; set; }
}

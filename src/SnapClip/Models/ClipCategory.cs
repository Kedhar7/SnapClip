namespace SnapClip.Models;

/// <summary>
/// Represents a category or tag that can be applied to clips.
/// </summary>
public sealed class ClipCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; }
}

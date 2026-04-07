namespace SnapClip.Models;

/// <summary>
/// Represents a single clipboard entry captured by SnapClip.
/// </summary>
public sealed class ClipItem
{
    public int Id { get; set; }

    /// <summary>
    /// Text content, file paths (newline-separated), or HTML content depending on <see cref="Type"/>.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Compressed PNG bytes for image clips; null for non-image types.
    /// </summary>
    public byte[]? ImageData { get; set; }

    /// <summary>
    /// 64x64 PNG thumbnail for list display.
    /// </summary>
    public byte[]? ThumbnailData { get; set; }

    public ClipType Type { get; set; }

    public DateTime CapturedAt { get; set; }

    public DateTime? LastPastedAt { get; set; }

    public int PasteCount { get; set; }

    public bool IsPinned { get; set; }

    public bool IsFavorite { get; set; }

    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Optional user-assigned or auto-detected category tag.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Title of the foreground window when the clip was captured.
    /// </summary>
    public string? SourceApplication { get; set; }
}

/// <summary>
/// The type of clipboard content captured.
/// </summary>
public enum ClipType
{
    Text,
    Image,
    File,
    RichText
}

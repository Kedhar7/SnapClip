namespace SnapClip.Models;

/// <summary>
/// Represents a local-only telemetry event for usage tracking.
/// </summary>
public sealed class TelemetryEvent
{
    public int Id { get; set; }

    /// <summary>
    /// Event name such as "clip_captured", "clip_pasted", "search_executed".
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON metadata associated with the event.
    /// </summary>
    public string? EventData { get; set; }

    public DateTime Timestamp { get; set; }
}

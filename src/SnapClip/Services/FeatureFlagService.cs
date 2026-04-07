using System.IO;
using System.Text.Json;

namespace SnapClip.Services;

/// <summary>
/// Local feature flag system for toggling experimental features.
/// Flags are persisted to a JSON file in the app data directory.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly string _flagsFilePath;
    private readonly object _lock = new();
    private Dictionary<string, bool> _flags;

    /// <summary>
    /// Well-known feature flag names.
    /// </summary>
    public static class Flags
    {
        public const string SmartCategorization = "SmartCategorization";
        public const string ImageOcr = "ImageOcr";
        public const string SensitiveContentDetection = "SensitiveContentDetection";
        public const string ClipMerge = "ClipMerge";
        public const string SoundEffects = "SoundEffects";
    }

    public FeatureFlagService(string appDataPath)
    {
        _flagsFilePath = Path.Combine(appDataPath, "features.json");
        _flags = LoadFlags();
    }

    /// <inheritdoc />
    public bool IsEnabled(string featureName)
    {
        lock (_lock)
        {
            return _flags.TryGetValue(featureName, out bool enabled) && enabled;
        }
    }

    /// <inheritdoc />
    public void SetFlag(string featureName, bool enabled)
    {
        Dictionary<string, bool> snapshot;
        lock (_lock)
        {
            _flags[featureName] = enabled;
            snapshot = new Dictionary<string, bool>(_flags);
        }
        WriteFlagsToFile(snapshot);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, bool> GetAllFlags()
    {
        lock (_lock)
        {
            return new Dictionary<string, bool>(_flags);
        }
    }

    private Dictionary<string, bool> LoadFlags()
    {
        try
        {
            if (File.Exists(_flagsFilePath))
            {
                string json = File.ReadAllText(_flagsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? GetDefaultFlags();
            }
        }
        catch
        {
            // Fall through to defaults
        }

        var defaults = GetDefaultFlags();
        SaveFlags(defaults);
        return defaults;
    }

    private void SaveFlags()
    {
        // Snapshot under lock, write outside lock to avoid holding lock during I/O
        Dictionary<string, bool> snapshot;
        lock (_lock)
        {
            snapshot = new Dictionary<string, bool>(_flags);
        }
        WriteFlagsToFile(snapshot);
    }

    private void SaveFlags(Dictionary<string, bool> flags)
    {
        WriteFlagsToFile(flags);
    }

    private void WriteFlagsToFile(Dictionary<string, bool> flags)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_flagsFilePath);
            if (directory is not null)
                Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(flags, options);
            File.WriteAllText(_flagsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save feature flags: {ex.Message}");
        }
    }

    private static Dictionary<string, bool> GetDefaultFlags()
    {
        return new Dictionary<string, bool>
        {
            [Flags.SmartCategorization] = true,
            [Flags.ImageOcr] = false,
            [Flags.SensitiveContentDetection] = true,
            [Flags.ClipMerge] = false,
            [Flags.SoundEffects] = false
        };
    }
}

/// <summary>
/// Interface for the feature flag service.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Returns whether the specified feature is enabled.
    /// </summary>
    bool IsEnabled(string featureName);

    /// <summary>
    /// Sets the enabled state for a feature flag and persists to disk.
    /// </summary>
    void SetFlag(string featureName, bool enabled);

    /// <summary>
    /// Returns all registered feature flags and their states.
    /// </summary>
    IReadOnlyDictionary<string, bool> GetAllFlags();
}

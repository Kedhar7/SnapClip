using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapClip.Models;
using SnapClip.Services;

namespace SnapClip.ViewModels;

/// <summary>
/// ViewModel for the usage insights/telemetry dashboard.
/// </summary>
public sealed partial class InsightsViewModel : ObservableObject
{
    private readonly TelemetryService _telemetryService;
    private readonly ClipStorageService _storageService;
    private readonly IFeatureFlagService _featureFlagService;

    [ObservableProperty]
    private int _clipsToday;

    [ObservableProperty]
    private int _clipsThisWeek;

    [ObservableProperty]
    private int _clipsAllTime;

    [ObservableProperty]
    private int _totalPastes;

    [ObservableProperty]
    private List<ClipItem> _mostUsedClips = [];

    [ObservableProperty]
    private Dictionary<int, int> _usageByHour = [];

    [ObservableProperty]
    private Dictionary<string, int> _clipsByType = [];

    [ObservableProperty]
    private Dictionary<string, bool> _featureFlags = [];

    public InsightsViewModel(
        TelemetryService telemetryService,
        ClipStorageService storageService,
        IFeatureFlagService featureFlagService)
    {
        _telemetryService = telemetryService;
        _storageService = storageService;
        _featureFlagService = featureFlagService;
    }

    /// <summary>
    /// Loads all insights data from storage.
    /// </summary>
    [RelayCommand]
    public async Task LoadInsightsAsync()
    {
        ClipsToday = await _telemetryService.GetTodayCountAsync("clip_captured").ConfigureAwait(false);
        ClipsThisWeek = await _telemetryService.GetWeekCountAsync("clip_captured").ConfigureAwait(false);
        ClipsAllTime = await _storageService.GetClipCountAsync().ConfigureAwait(false);
        TotalPastes = await _storageService.GetTotalPasteCountAsync().ConfigureAwait(false);
        UsageByHour = await _telemetryService.GetUsageByHourAsync().ConfigureAwait(false);

        // Get most-used clips (top 5 by paste count)
        var allClips = await _storageService.GetClipsAsync(1000).ConfigureAwait(false);

        MostUsedClips = allClips
            .OrderByDescending(c => c.PasteCount)
            .Take(5)
            .ToList();

        ClipsByType = allClips
            .GroupBy(c => c.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        FeatureFlags = new Dictionary<string, bool>(_featureFlagService.GetAllFlags());
    }
}

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapClip.Helpers;
using SnapClip.Models;
using SnapClip.Services;

namespace SnapClip.ViewModels;

/// <summary>
/// Primary ViewModel for the main clip history window.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ClipStorageService _storageService;
    private readonly SearchService _searchService;
    private readonly TelemetryService _telemetryService;
    private readonly ClipboardListenerService _clipboardListener;
    private CancellationTokenSource? _searchDebounceToken;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClipItemViewModel? _selectedClip;

    [ObservableProperty]
    private int _clipCount;

    [ObservableProperty]
    private int _totalPastes;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ClipItemViewModel> PinnedClips { get; } = [];
    public ObservableCollection<ClipItemViewModel> RecentClips { get; } = [];

    /// <summary>
    /// Fired when a clip is selected for pasting and the window should close.
    /// </summary>
    public event EventHandler? PasteRequested;

    /// <summary>
    /// The handle of the window that was active before SnapClip was shown.
    /// </summary>
    public IntPtr PreviousWindowHandle { get; set; }

    public MainViewModel(
        ClipStorageService storageService,
        SearchService searchService,
        TelemetryService telemetryService,
        ClipboardListenerService clipboardListener)
    {
        _storageService = storageService;
        _searchService = searchService;
        _telemetryService = telemetryService;
        _clipboardListener = clipboardListener;

        _clipboardListener.ClipCaptured += OnClipCaptured;
    }

    /// <summary>
    /// Loads clips from storage and builds the search index.
    /// </summary>
    public async Task InitializeAsync()
    {
        var clips = await _storageService.GetClipsAsync(1000).ConfigureAwait(false);
        _searchService.RebuildIndex(clips);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            RefreshClipLists(clips);
        });

        ClipCount = await _storageService.GetClipCountAsync().ConfigureAwait(false);
        TotalPastes = await _storageService.GetTotalPasteCountAsync().ConfigureAwait(false);
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceToken?.Cancel();
        _searchDebounceToken = new CancellationTokenSource();
        var token = _searchDebounceToken.Token;

        _ = DebounceSearchAsync(value, token);
    }

    private async Task DebounceSearchAsync(string query, CancellationToken token)
    {
        try
        {
            await Task.Delay(150, token).ConfigureAwait(false);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var results = _searchService.Search(query);
            sw.Stop();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RefreshClipLists(results);
                StatusText = string.IsNullOrEmpty(query)
                    ? $"Showing {results.Count} clips"
                    : $"{results.Count} results ({sw.ElapsedMilliseconds}ms)";
            });

            await _telemetryService.LogEventAsync("search_executed", new
            {
                query_length = query.Length,
                result_count = results.Count,
                latency_ms = sw.ElapsedMilliseconds
            }).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            // Expected when user types another character before debounce completes
        }
    }

    /// <summary>
    /// Selects a clip: copies it to clipboard and pastes to previous window.
    /// </summary>
    [RelayCommand]
    private async Task SelectClipAsync(ClipItemViewModel? clipVm)
    {
        if (clipVm is null) return;

        // Copy to clipboard
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            switch (clipVm.Type)
            {
                case ClipType.Image when clipVm.Model.ImageData is not null:
                    var image = ImageHelper.LoadFromBytes(clipVm.Model.ImageData);
                    Clipboard.SetImage(image);
                    break;
                case ClipType.File:
                    var files = clipVm.Content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                    var fileCollection = new System.Collections.Specialized.StringCollection();
                    fileCollection.AddRange(files);
                    Clipboard.SetFileDropList(fileCollection);
                    break;
                default:
                    Clipboard.SetText(clipVm.Content);
                    break;
            }
        });

        // Record paste
        await _storageService.RecordPasteAsync(clipVm.Id).ConfigureAwait(false);
        clipVm.PasteCount++;
        TotalPastes++;

        await _telemetryService.LogEventAsync("clip_pasted", new
        {
            clip_type = clipVm.Type.ToString(),
            clip_age_seconds = (DateTime.UtcNow - clipVm.CapturedAt).TotalSeconds,
            paste_method = "click"
        }).ConfigureAwait(false);

        PasteRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Toggles the pinned state of a clip.
    /// </summary>
    [RelayCommand]
    private async Task PinClipAsync(ClipItemViewModel? clipVm)
    {
        if (clipVm is null) return;

        await _storageService.TogglePinAsync(clipVm.Id).ConfigureAwait(false);
        clipVm.IsPinned = !clipVm.IsPinned;

        await _telemetryService.LogEventAsync("clip_pinned", new
        {
            pinned = clipVm.IsPinned
        }).ConfigureAwait(false);

        // Refresh lists to move between pinned/recent sections
        var results = _searchService.Search(SearchText);
        await Application.Current.Dispatcher.InvokeAsync(() => RefreshClipLists(results));
    }

    /// <summary>
    /// Deletes a clip from history.
    /// </summary>
    [RelayCommand]
    private async Task DeleteClipAsync(ClipItemViewModel? clipVm)
    {
        if (clipVm is null) return;

        await _storageService.DeleteClipAsync(clipVm.Id).ConfigureAwait(false);
        _searchService.RemoveFromIndex(clipVm.Id);

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            PinnedClips.Remove(clipVm);
            RecentClips.Remove(clipVm);
            ClipCount--;
        });

        await _telemetryService.LogEventAsync("clip_deleted").ConfigureAwait(false);
    }

    /// <summary>
    /// Toggles the favorite state of a clip.
    /// </summary>
    [RelayCommand]
    private async Task FavoriteClipAsync(ClipItemViewModel? clipVm)
    {
        if (clipVm is null) return;

        await _storageService.ToggleFavoriteAsync(clipVm.Id).ConfigureAwait(false);
        clipVm.IsFavorite = !clipVm.IsFavorite;
    }

    private void OnClipCaptured(object? sender, ClipItem clip)
    {
        _searchService.AddToIndex(clip);

        // Event is raised on UI thread by ClipboardListenerService
        var vm = new ClipItemViewModel(clip);
        RecentClips.Insert(0, vm);
        ClipCount++;
    }

    private void RefreshClipLists(List<ClipItem> clips)
    {
        PinnedClips.Clear();
        RecentClips.Clear();

        foreach (var clip in clips)
        {
            var vm = new ClipItemViewModel(clip);
            if (clip.IsPinned)
                PinnedClips.Add(vm);
            else
                RecentClips.Add(vm);
        }

        ClipCount = PinnedClips.Count + RecentClips.Count;
    }
}

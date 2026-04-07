using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapClip.Services;

namespace SnapClip.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ClipStorageService _storageService;
    private readonly ThemeService _themeService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly string _settingsFilePath;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private int _maxHistorySize = 1000;

    [ObservableProperty]
    private AppTheme _selectedTheme = AppTheme.SystemDefault;

    [ObservableProperty]
    private int _fontSize = 14;

    [ObservableProperty]
    private bool _autoClearOnExit;

    [ObservableProperty]
    private string _excludedApps = string.Empty;

    [ObservableProperty]
    private string _encryptionPassphrase = string.Empty;

    [ObservableProperty]
    private bool _developerMode;

    // Feature flags
    [ObservableProperty]
    private bool _smartCategorization;

    [ObservableProperty]
    private bool _imageOcr;

    [ObservableProperty]
    private bool _sensitiveContentDetection;

    [ObservableProperty]
    private bool _clipMerge;

    [ObservableProperty]
    private bool _soundEffects;

    public string Version => "1.0.0";

    public SettingsViewModel(
        ClipStorageService storageService,
        ThemeService themeService,
        IFeatureFlagService featureFlagService,
        string appDataPath)
    {
        _storageService = storageService;
        _themeService = themeService;
        _featureFlagService = featureFlagService;
        _settingsFilePath = Path.Combine(appDataPath, "appsettings.json");

        LoadSettings();
        LoadFeatureFlags();
    }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _themeService.CurrentTheme = value;
    }

    partial void OnMaxHistorySizeChanged(int value)
    {
        _storageService.MaxHistorySize = value;
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _storageService.ClearHistoryAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void SaveSettings()
    {
        SaveFeatureFlags();
        PersistSettings();
        UpdateStartWithWindows();
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath)) return;

            string json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<SettingsData>(json);
            if (settings is null) return;

            StartWithWindows = settings.StartWithWindows;
            MaxHistorySize = settings.MaxHistorySize;
            SelectedTheme = settings.Theme;
            FontSize = settings.FontSize;
            AutoClearOnExit = settings.AutoClearOnExit;
            ExcludedApps = settings.ExcludedApps ?? string.Empty;
            DeveloperMode = settings.DeveloperMode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
    }

    private void PersistSettings()
    {
        try
        {
            var settings = new SettingsData
            {
                StartWithWindows = StartWithWindows,
                MaxHistorySize = MaxHistorySize,
                Theme = SelectedTheme,
                FontSize = FontSize,
                AutoClearOnExit = AutoClearOnExit,
                ExcludedApps = ExcludedApps,
                DeveloperMode = DeveloperMode
            };

            string directory = Path.GetDirectoryName(_settingsFilePath)!;
            Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private void LoadFeatureFlags()
    {
        var flags = _featureFlagService.GetAllFlags();
        SmartCategorization = flags.GetValueOrDefault(FeatureFlagService.Flags.SmartCategorization);
        ImageOcr = flags.GetValueOrDefault(FeatureFlagService.Flags.ImageOcr);
        SensitiveContentDetection = flags.GetValueOrDefault(FeatureFlagService.Flags.SensitiveContentDetection);
        ClipMerge = flags.GetValueOrDefault(FeatureFlagService.Flags.ClipMerge);
        SoundEffects = flags.GetValueOrDefault(FeatureFlagService.Flags.SoundEffects);
    }

    private void SaveFeatureFlags()
    {
        _featureFlagService.SetFlag(FeatureFlagService.Flags.SmartCategorization, SmartCategorization);
        _featureFlagService.SetFlag(FeatureFlagService.Flags.ImageOcr, ImageOcr);
        _featureFlagService.SetFlag(FeatureFlagService.Flags.SensitiveContentDetection, SensitiveContentDetection);
        _featureFlagService.SetFlag(FeatureFlagService.Flags.ClipMerge, ClipMerge);
        _featureFlagService.SetFlag(FeatureFlagService.Flags.SoundEffects, SoundEffects);
    }

    private void UpdateStartWithWindows()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if (key is null) return;

            if (StartWithWindows)
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue("SnapClip", $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue("SnapClip", false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update startup registry: {ex.Message}");
        }
    }

    private sealed class SettingsData
    {
        public bool StartWithWindows { get; set; }
        public int MaxHistorySize { get; set; } = 1000;
        public AppTheme Theme { get; set; } = AppTheme.SystemDefault;
        public int FontSize { get; set; } = 14;
        public bool AutoClearOnExit { get; set; }
        public string? ExcludedApps { get; set; }
        public bool DeveloperMode { get; set; }
    }
}

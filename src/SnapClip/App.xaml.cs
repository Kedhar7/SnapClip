using System.IO;
using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapClip.Data;
using SnapClip.Services;
using SnapClip.ViewModels;
using SnapClip.Views;

namespace SnapClip;

/// <summary>
/// Application entry point. Configures DI container, system tray, and global hotkeys.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private HotkeyService? _hotkeyService;
    private ClipboardListenerService? _clipboardListener;

    public IServiceProvider ServiceProvider => _serviceProvider!;

    private static string AppDataPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnapClip");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory(AppDataPath);

        ConfigureServices();
        await InitializeDatabaseAsync();
        await CreateMainWindowAsync();
        SetupSystemTray();
        SetupHotkeys();
        StartClipboardListener();

        var telemetry = _serviceProvider!.GetRequiredService<TelemetryService>();
        await telemetry.LogEventAsync("app_session_start");

        var notification = _serviceProvider!.GetRequiredService<NotificationService>();
        notification.ShowStartupNotification();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Database
        string dbPath = Path.Combine(AppDataPath, "snapclip.db");
        services.AddDbContextFactory<SnapClipDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Services
        services.AddSingleton<ClipStorageService>();
        services.AddSingleton<SearchService>();
        services.AddSingleton<ClipboardListenerService>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<TelemetryService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IFeatureFlagService>(_ => new FeatureFlagService(AppDataPath));

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(
            sp.GetRequiredService<ClipStorageService>(),
            sp.GetRequiredService<ThemeService>(),
            sp.GetRequiredService<IFeatureFlagService>(),
            AppDataPath));
        services.AddTransient<InsightsViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<InsightsWindow>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private async Task InitializeDatabaseAsync()
    {
        var factory = _serviceProvider!.GetRequiredService<IDbContextFactory<SnapClipDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private async Task CreateMainWindowAsync()
    {
        _mainWindow = _serviceProvider!.GetRequiredService<MainWindow>();

        // Load clip history and build search index
        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        await viewModel.InitializeAsync();
    }

    private void SetupSystemTray()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "SnapClip — Smart Clipboard Manager",
            MenuActivation = PopupActivationMode.RightClick,
            DoubleClickCommand = new RelayCommand(ToggleMainWindow)
        };

        var contextMenu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "Show SnapClip" };
        showItem.Click += (_, _) => ToggleMainWindow();

        var pauseItem = new System.Windows.Controls.MenuItem { Header = "Pause Monitoring" };
        pauseItem.Click += (_, _) =>
        {
            if (_clipboardListener is not null)
            {
                _clipboardListener.IsPaused = !_clipboardListener.IsPaused;
                pauseItem.Header = _clipboardListener.IsPaused ? "Resume Monitoring" : "Pause Monitoring";
            }
        };

        var clearItem = new System.Windows.Controls.MenuItem { Header = "Clear History" };
        clearItem.Click += async (_, _) =>
        {
            var storage = _serviceProvider!.GetRequiredService<ClipStorageService>();
            await storage.ClearHistoryAsync();
        };

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
        settingsItem.Click += (_, _) =>
        {
            var window = _serviceProvider!.GetRequiredService<SettingsWindow>();
            window.ShowDialog();
        };

        var insightsItem = new System.Windows.Controls.MenuItem { Header = "Insights" };
        insightsItem.Click += (_, _) =>
        {
            var window = _serviceProvider!.GetRequiredService<InsightsWindow>();
            window.Show();
        };

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(pauseItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(clearItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(insightsItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;
    }

    private void SetupHotkeys()
    {
        if (_mainWindow is null) return;

        // Need to show window briefly to get HWND, then hide
        _mainWindow.Show();
        _mainWindow.Hide();

        _hotkeyService = _serviceProvider!.GetRequiredService<HotkeyService>();
        _hotkeyService.Initialize(_mainWindow);

        var notification = _serviceProvider!.GetRequiredService<NotificationService>();
        _hotkeyService.HotkeyConflict += (_, hotkey) => notification.ShowHotkeyConflict(hotkey);

        // Register default hotkeys
        _hotkeyService.RegisterHotkey(ModifierKeys.Control | ModifierKeys.Shift, Key.V, ToggleMainWindow);
        _hotkeyService.RegisterHotkey(ModifierKeys.Control | ModifierKeys.Shift, Key.D, DeleteLastClip);
        _hotkeyService.RegisterHotkey(ModifierKeys.Control | ModifierKeys.Shift, Key.P, PinLastClip);
    }

    private void StartClipboardListener()
    {
        if (_mainWindow is null) return;

        _clipboardListener = _serviceProvider!.GetRequiredService<ClipboardListenerService>();
        _clipboardListener.Start(_mainWindow);
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow is null) return;

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.ShowAndFocus();
        }
    }

    private async void DeleteLastClip()
    {
        var vm = _serviceProvider!.GetRequiredService<MainViewModel>();
        var lastClip = vm.RecentClips.FirstOrDefault();
        if (lastClip is not null)
        {
            await vm.DeleteClipCommand.ExecuteAsync(lastClip);
        }

        var telemetry = _serviceProvider!.GetRequiredService<TelemetryService>();
        await telemetry.LogEventAsync("hotkey_used", new { hotkey = "Ctrl+Shift+D" });
    }

    private async void PinLastClip()
    {
        var vm = _serviceProvider!.GetRequiredService<MainViewModel>();
        var lastClip = vm.RecentClips.FirstOrDefault();
        if (lastClip is not null)
        {
            await vm.PinClipCommand.ExecuteAsync(lastClip);
        }

        var telemetry = _serviceProvider!.GetRequiredService<TelemetryService>();
        await telemetry.LogEventAsync("hotkey_used", new { hotkey = "Ctrl+Shift+P" });
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var telemetry = _serviceProvider?.GetService<TelemetryService>();
        if (telemetry is not null)
        {
            await telemetry.LogEventAsync("app_session_end");
        }

        _clipboardListener?.Dispose();
        _hotkeyService?.Dispose();
        _trayIcon?.Dispose();
        _serviceProvider?.Dispose();

        base.OnExit(e);
    }

    /// <summary>
    /// Simple relay command for tray icon double-click.
    /// </summary>
    private sealed class RelayCommand(Action execute) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}

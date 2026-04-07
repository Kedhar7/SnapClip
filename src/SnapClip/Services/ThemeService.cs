using System.Windows;

namespace SnapClip.Services;

/// <summary>
/// Manages application theming (Light, Dark, High Contrast, System Default).
/// </summary>
public sealed class ThemeService
{
    private const string ThemeBasePath = "Resources/Themes/";
    private AppTheme _currentTheme = AppTheme.SystemDefault;

    /// <summary>
    /// Fired when the theme changes.
    /// </summary>
    public event EventHandler<AppTheme>? ThemeChanged;

    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ApplyTheme(value);
                ThemeChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    public void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app is null) return;

        // Remove existing theme dictionaries
        var toRemove = app.Resources.MergedDictionaries
            .Where(d => d.Source?.OriginalString.Contains("Theme") == true)
            .ToList();

        foreach (var dict in toRemove)
            app.Resources.MergedDictionaries.Remove(dict);

        // Determine which theme to apply
        var effectiveTheme = theme;
        if (theme == AppTheme.SystemDefault)
        {
            effectiveTheme = SystemParameters.HighContrast
                ? AppTheme.HighContrast
                : IsSystemDarkMode() ? AppTheme.Dark : AppTheme.Light;
        }

        string themeFile = effectiveTheme switch
        {
            AppTheme.Dark => "DarkTheme.xaml",
            AppTheme.HighContrast => "HighContrastTheme.xaml",
            _ => "LightTheme.xaml"
        };

        var themeUri = new Uri($"{ThemeBasePath}{themeFile}", UriKind.Relative);
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }

    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Available application themes.
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
    HighContrast,
    SystemDefault
}

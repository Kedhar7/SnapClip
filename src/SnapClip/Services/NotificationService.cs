using Microsoft.Toolkit.Uwp.Notifications;

namespace SnapClip.Services;

/// <summary>
/// Windows toast notification service for key application events.
/// </summary>
public sealed class NotificationService
{
    /// <summary>
    /// Shows a toast notification with a title and message.
    /// </summary>
    public void ShowNotification(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Toast notification failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the startup notification.
    /// </summary>
    public void ShowStartupNotification()
    {
        ShowNotification("SnapClip is running", "Monitoring your clipboard. Press Ctrl+Shift+V to open.");
    }

    /// <summary>
    /// Shows a notification when sensitive content is auto-encrypted.
    /// </summary>
    public void ShowSensitiveContentDetected()
    {
        ShowNotification("Sensitive content detected", "The clip has been automatically encrypted for your protection.");
    }

    /// <summary>
    /// Shows a notification when a hotkey conflicts with another application.
    /// </summary>
    public void ShowHotkeyConflict(string hotkey)
    {
        ShowNotification("Hotkey conflict", $"The shortcut {hotkey} is already in use by another application. Please choose a different shortcut in Settings.");
    }

    /// <summary>
    /// Shows a notification when history cleanup occurs.
    /// </summary>
    public void ShowHistoryCleanup(int removedCount)
    {
        ShowNotification("History cleanup", $"Removed {removedCount} old clips to stay within your history limit.");
    }
}

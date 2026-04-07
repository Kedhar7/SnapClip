using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SnapClip.Helpers;
using SnapClip.Models;

namespace SnapClip.Services;

/// <summary>
/// Monitors the Windows clipboard for changes using the Win32 clipboard listener API.
/// </summary>
public sealed class ClipboardListenerService : IDisposable
{
    private readonly ClipStorageService _storageService;
    private readonly object _hashLock = new();
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private bool _isListening;
    private volatile bool _isPaused;
    private string? _lastClipHash;

    /// <summary>
    /// Fired when a new clip is captured from the clipboard.
    /// </summary>
    public event EventHandler<ClipItem>? ClipCaptured;

    public bool IsPaused
    {
        get => _isPaused;
        set => _isPaused = value;
    }

    public ClipboardListenerService(ClipStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// Starts listening for clipboard changes on the given window.
    /// </summary>
    public void Start(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;
        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(WndProc);

        if (!Win32Interop.AddClipboardFormatListener(_hwnd))
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to register clipboard listener. Win32 error: {error}");
        }

        _isListening = true;
    }

    /// <summary>
    /// Stops listening for clipboard changes.
    /// </summary>
    public void Stop()
    {
        if (_isListening && _hwnd != IntPtr.Zero)
        {
            Win32Interop.RemoveClipboardFormatListener(_hwnd);
            _hwndSource?.RemoveHook(WndProc);
            _isListening = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Interop.WM_CLIPBOARDUPDATE && !_isPaused)
        {
            handled = true;
            _ = Task.Run(ProcessClipboardChangeAsync);
        }
        return IntPtr.Zero;
    }

    private async Task ProcessClipboardChangeAsync()
    {
        try
        {
            ClipItem? clip = null;

            // Must access clipboard on STA thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                clip = CaptureClipboardContent();
            });

            if (clip is null)
                return;

            // Deduplicate with thread-safe hash check
            string hash = ComputeClipHash(clip);
            lock (_hashLock)
            {
                if (hash == _lastClipHash)
                    return;
                _lastClipHash = hash;
            }

            // Capture source application
            clip.SourceApplication = Win32Interop.GetForegroundWindowTitle();
            clip.CapturedAt = DateTime.UtcNow;

            await _storageService.SaveClipAsync(clip).ConfigureAwait(false);

            // Raise event on UI thread so handlers can safely update UI
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ClipCaptured?.Invoke(this, clip);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard capture error: {ex.Message}");
        }
    }

    private static ClipItem? CaptureClipboardContent()
    {
        var data = Clipboard.GetDataObject();
        if (data is null)
            return null;

        // Try HTML / Rich Text first
        if (data.GetDataPresent(DataFormats.Html))
        {
            string? html = data.GetData(DataFormats.Html) as string;
            if (!string.IsNullOrWhiteSpace(html))
            {
                return new ClipItem
                {
                    Content = html,
                    Type = ClipType.RichText
                };
            }
        }

        // Image
        if (data.GetDataPresent(DataFormats.Bitmap))
        {
            var bitmapSource = Clipboard.GetImage();
            if (bitmapSource is not null)
            {
                byte[] imageData = ImageHelper.CompressToPng(bitmapSource);
                byte[] thumbnail = ImageHelper.GenerateThumbnail(bitmapSource);
                return new ClipItem
                {
                    Content = $"[Image {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight}]",
                    ImageData = imageData,
                    ThumbnailData = thumbnail,
                    Type = ClipType.Image
                };
            }
        }

        // File drop
        if (data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = data.GetData(DataFormats.FileDrop) as string[];
            if (files is { Length: > 0 })
            {
                return new ClipItem
                {
                    Content = string.Join(Environment.NewLine, files),
                    Type = ClipType.File
                };
            }
        }

        // Plain text (most common, checked last as fallback)
        if (data.GetDataPresent(DataFormats.UnicodeText))
        {
            string? text = data.GetData(DataFormats.UnicodeText) as string;
            if (!string.IsNullOrEmpty(text))
            {
                return new ClipItem
                {
                    Content = text,
                    Type = ClipType.Text
                };
            }
        }

        return null;
    }

    private static string ComputeClipHash(ClipItem clip)
    {
        if (clip.ImageData is not null)
            return $"img:{clip.ImageData.Length}:{Convert.ToBase64String(clip.ImageData[..Math.Min(64, clip.ImageData.Length)])}";

        return $"{clip.Type}:{clip.Content}";
    }

    public void Dispose()
    {
        Stop();
    }
}

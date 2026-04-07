using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapClip.Converters;
using SnapClip.Models;

namespace SnapClip.ViewModels;

/// <summary>
/// ViewModel wrapper for a single <see cref="ClipItem"/> in the UI list.
/// </summary>
public sealed partial class ClipItemViewModel : ObservableObject
{
    private readonly ClipItem _model;

    public ClipItemViewModel(ClipItem model)
    {
        _model = model;
    }

    public ClipItem Model => _model;
    public int Id => _model.Id;
    public string Content => _model.Content;
    public ClipType Type => _model.Type;
    public DateTime CapturedAt => _model.CapturedAt;
    public string? SourceApplication => _model.SourceApplication;
    public string? Category => _model.Category;

    /// <summary>
    /// Display-friendly content preview (first 100 characters for text).
    /// </summary>
    public string Preview => Type switch
    {
        ClipType.Text => Content.Length > 100 ? Content[..100] + "..." : Content,
        ClipType.RichText => Content.Length > 100 ? Content[..100] + "..." : Content,
        ClipType.File => FormatFilePaths(Content),
        ClipType.Image => Content, // e.g., "[Image 1920x1080]"
        _ => Content
    };

    /// <summary>
    /// Relative timestamp string (e.g., "2m ago").
    /// </summary>
    public string RelativeTime => TimestampToRelativeConverter.FormatRelativeTime(CapturedAt);

    public bool IsPinned
    {
        get => _model.IsPinned;
        set
        {
            if (_model.IsPinned != value)
            {
                _model.IsPinned = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsFavorite
    {
        get => _model.IsFavorite;
        set
        {
            if (_model.IsFavorite != value)
            {
                _model.IsFavorite = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsEncrypted
    {
        get => _model.IsEncrypted;
        set
        {
            if (_model.IsEncrypted != value)
            {
                _model.IsEncrypted = value;
                OnPropertyChanged();
            }
        }
    }

    public int PasteCount
    {
        get => _model.PasteCount;
        set
        {
            if (_model.PasteCount != value)
            {
                _model.PasteCount = value;
                OnPropertyChanged();
            }
        }
    }

    private BitmapSource? _cachedThumbnail;
    private bool _thumbnailLoaded;

    /// <summary>
    /// Thumbnail image for image clips. Cached after first load.
    /// </summary>
    public BitmapSource? Thumbnail
    {
        get
        {
            if (_thumbnailLoaded)
                return _cachedThumbnail;

            _thumbnailLoaded = true;

            if (_model.ThumbnailData is null || _model.ThumbnailData.Length == 0)
                return null;

            try
            {
                using var stream = new MemoryStream(_model.ThumbnailData);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze(); // Makes it thread-safe and detaches from stream
                _cachedThumbnail = image;
                return _cachedThumbnail;
            }
            catch
            {
                return null;
            }
        }
    }

    private static string FormatFilePaths(string content)
    {
        var paths = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length == 1)
            return Path.GetFileName(paths[0]);

        return string.Join(", ", paths.Select(Path.GetFileName));
    }
}

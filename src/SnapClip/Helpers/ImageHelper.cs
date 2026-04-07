using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnapClip.Helpers;

/// <summary>
/// Utilities for image compression and thumbnail generation.
/// </summary>
internal static class ImageHelper
{
    private const int ThumbnailSize = 64;
    private const int MaxWidth = 1920;
    private const int MaxHeight = 1080;

    /// <summary>
    /// Compresses a BitmapSource to PNG bytes, capping dimensions at 1920x1080.
    /// </summary>
    public static byte[] CompressToPng(BitmapSource source)
    {
        var resized = ResizeIfNeeded(source, MaxWidth, MaxHeight);
        return EncodeToPng(resized);
    }

    /// <summary>
    /// Generates a 64x64 PNG thumbnail from a BitmapSource.
    /// </summary>
    public static byte[] GenerateThumbnail(BitmapSource source)
    {
        var thumbnail = ResizeIfNeeded(source, ThumbnailSize, ThumbnailSize);
        return EncodeToPng(thumbnail);
    }

    /// <summary>
    /// Generates a 64x64 PNG thumbnail from full-size PNG bytes.
    /// </summary>
    public static byte[] GenerateThumbnail(byte[] imageData)
    {
        var source = LoadFromBytes(imageData);
        return GenerateThumbnail(source);
    }

    /// <summary>
    /// Loads a BitmapSource from PNG byte data.
    /// </summary>
    public static BitmapSource LoadFromBytes(byte[] data)
    {
        using var stream = new MemoryStream(data);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }

    private static BitmapSource ResizeIfNeeded(BitmapSource source, int maxWidth, int maxHeight)
    {
        if (source.PixelWidth <= maxWidth && source.PixelHeight <= maxHeight)
            return source;

        double scaleX = (double)maxWidth / source.PixelWidth;
        double scaleY = (double)maxHeight / source.PixelHeight;
        double scale = Math.Min(scaleX, scaleY);

        var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        return transformed;
    }

    private static byte[] EncodeToPng(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}

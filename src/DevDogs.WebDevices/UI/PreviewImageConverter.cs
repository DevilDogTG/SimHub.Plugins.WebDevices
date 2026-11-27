using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevDogs.WebDevices.UI;

/// <summary>
/// Turns a dashboard preview path into a small picture. Each file is read once, shrunk and shared by every
/// picker; the file is not kept open, so SimHub can still replace it.
/// </summary>
public sealed class PreviewImageConverter : IValueConverter
{
    // About twice the list's 96 px preview width, for sharp pictures on scaled displays.
    private const int DecodeWidth = 192;

    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || path.Length == 0)
        {
            return null;
        }

        if (!Cache.TryGetValue(path, out var image))
        {
            Cache[path] = image = Load(path);
        }

        return image;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static ImageSource? Load(string path)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.DecodePixelWidth = DecodeWidth;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or FormatException
                                       or InvalidOperationException or ArgumentException)
        {
            // A broken or unreadable picture shows as an empty preview.
            return null;
        }
    }
}

using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace UnpackerGui.Converters;

public class PathConverter : IValueConverter
{
    private const double MaxWidth = 400.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        string path => FormatDisplayPath(path),
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;

    /// <summary>
    /// Formats the given path to display within the width limitation.
    /// </summary>
    private static string FormatDisplayPath(string path)
    {
        path = NormalizePath(path, out string[] parts);

        if (!App.TryGetResource("ContentControlThemeFontFamily", out FontFamily? fontFamily)) return path;
        if (!App.TryGetResource("ControlContentThemeFontSize", out double fontSize)) return path;

        double width = GetDisplayWidth(path, fontFamily, fontSize);
        return width > MaxWidth ? ShortenPath(path, parts, fontFamily, fontSize) : path;
    }

    /// <summary>
    /// Normalizes the specified path.
    /// </summary>
    private static string NormalizePath(string path, out string[] parts)
    {
        parts = SplitPath(path);
        return Path.Join(parts);
    }

    /// <summary>
    /// Shortens the specified path to fit within the width limitation, if possible.
    /// </summary>
    private static string ShortenPath(string path, string[] parts, FontFamily fontFamily, double fontSize)
    {
        if (parts.Length < 3) return path;

        parts[1] = "...";
        path = Path.Join(parts);

        for (int i = 2; i < parts.Length - 1 && GetDisplayWidth(path, fontFamily, fontSize) > MaxWidth; i++)
        {
            parts[i] = null!;
            path = Path.Join(parts);
        }

        return path;
    }

    /// <summary>
    /// Gets the display width of the specified text.
    /// </summary>
    /// <remarks>Source: <see href="https://stackoverflow.com/a/78945115/20126793"/></remarks>
    private static double GetDisplayWidth(string text, FontFamily fontFamily, double fontSize)
    {
        TextShaper ts = TextShaper.Current;
        Typeface typeface = new(fontFamily);
        ShapedBuffer shaped = ts.ShapeText(text, new TextShaperOptions(typeface.GlyphTypeface, fontSize));
        ShapedTextRun run = new(shaped, new GenericTextRunProperties(typeface, fontSize));
        Size size = run.Size;
        return size.Width;
    }

    /// <summary>
    /// Splits the specified path into its individual parts.
    /// </summary>
    private static string[] SplitPath(ReadOnlySpan<char> path)
    {
        if (path.Length == 0) return [];

        Stack<string> parts = [];
        ReadOnlySpan<char> root = Path.GetPathRoot(path.ToString());

        while (path.Length > root.Length)
        {
            parts.Push(Path.GetFileName(path).ToString());
            path = Path.GetDirectoryName(path);
        }

        if (root.Length > 0)
        {
            parts.Push(root.ToString());
        }

        return [.. parts];
    }
}

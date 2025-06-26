using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UnpackerGui.Converters;

public class FileSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        sbyte n => GetFileSize(n),
        byte n => GetFileSize(n),
        short n => GetFileSize(n),
        ushort n => GetFileSize(n),
        int n => GetFileSize(n),
        uint n => GetFileSize(n),
        long n => GetFileSize(n),
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;

    public static string GetFileSize(long i) => (i < 0 ? -i : i) switch
    {
        >= 1L << 60 => $"{(i >> 50) / 1024.0:0.##} EB",
        >= 1L << 50 => $"{(i >> 40) / 1024.0:0.##} PB",
        >= 1L << 40 => $"{(i >> 30) / 1024.0:0.##} TB",
        >= 1L << 30 => $"{(i >> 20) / 1024.0:0.##} GB",
        >= 1L << 20 => $"{(i >> 10) / 1024.0:0.##} MB",
        >= 1L << 10 => $"{i / 1024.0:0.##} KB",
        _ => $"{i} B"
    };
}

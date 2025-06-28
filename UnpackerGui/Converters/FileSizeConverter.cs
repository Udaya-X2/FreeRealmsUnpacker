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
        ulong n => GetFileSize(n),
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;

    public static string GetFileSize(ulong i) => i switch
    {
        >= 1UL << 60 => $"{(i >> 50) / 1024.0:0.##} EB",
        >= 1UL << 50 => $"{(i >> 40) / 1024.0:0.##} PB",
        >= 1UL << 40 => $"{(i >> 30) / 1024.0:0.##} TB",
        >= 1UL << 30 => $"{(i >> 20) / 1024.0:0.##} GB",
        >= 1UL << 20 => $"{(i >> 10) / 1024.0:0.##} MB",
        >= 1UL << 10 => $"{i / 1024.0:0.##} KB",
        _ => $"{i} B"
    };

    public static string GetFileSize(long i) => i switch
    {
        >= 1L << 60 => $"{(i >> 50) / 1024.0:0.##} EB",
        >= 1L << 50 => $"{(i >> 40) / 1024.0:0.##} PB",
        >= 1L << 40 => $"{(i >> 30) / 1024.0:0.##} TB",
        >= 1L << 30 => $"{(i >> 20) / 1024.0:0.##} GB",
        >= 1L << 20 => $"{(i >> 10) / 1024.0:0.##} MB",
        >= 1L << 10 => $"{i / 1024.0:0.##} KB",
        _ => $"{i} B"
    };

    public static string GetFileSize(uint i) => i switch
    {
        >= 1u << 30 => $"{(i >> 20) / 1024.0:0.##} GB",
        >= 1u << 20 => $"{(i >> 10) / 1024.0:0.##} MB",
        >= 1u << 10 => $"{i / 1024.0:0.##} KB",
        _ => $"{i} B"
    };

    public static string GetFileSize(int i) => i switch
    {
        >= 1 << 30 => $"{(i >> 20) / 1024.0:0.##} GB",
        >= 1 << 20 => $"{(i >> 10) / 1024.0:0.##} MB",
        >= 1 << 10 => $"{i / 1024.0:0.##} KB",
        _ => $"{i} B"
    };

    public static string GetFileSize(ushort i) => i switch
    {
        >= 1 << 10 => $"{i / 1024.0:0.##} KB",
        _ => $"{i} B"
    };

    public static string GetFileSize(short i) => i switch
    {
        >= 1 << 10 => $"{i / 1024.0:0.##} KB",
        _ => $"{i} B"
    };

    public static string GetFileSize(byte i) => $"{i} B";

    public static string GetFileSize(sbyte i) => $"{i} B";
}

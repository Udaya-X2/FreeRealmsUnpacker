using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UnpackerGui.Converters;

public class LongToTimestampConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        long ms => CreateTimestamp(ms),
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.None)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;

    private static string CreateTimestamp(long ms)
    {
        TimeSpan time = new(ms * TimeSpan.TicksPerMillisecond);
        return time.Ticks switch
        {
            < TimeSpan.TicksPerSecond => "0:00",
            < TimeSpan.TicksPerMinute => $"0:{time.Seconds:D2}",
            < TimeSpan.TicksPerHour => $"{time.Minutes:D2}:{time.Seconds:D2}",
            < TimeSpan.TicksPerDay => $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}",
            _ => $"{time.Days:D2}:{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}",
        };
    }
}

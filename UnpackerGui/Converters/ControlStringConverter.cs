using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UnpackerGui.Converters;

public class ControlStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        "\r\n" => @"CR LF (\r\n)",
        "\n" => @"LF (\n)",
        "\r" => @"CR (\r)",
        _ => value
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

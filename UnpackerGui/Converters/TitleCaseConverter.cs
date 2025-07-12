using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace UnpackerGui.Converters;

public partial class TitleCaseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ToTitleCase($"{value}");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;

    private static string ToTitleCase(string value)
    {
        return WordRegex().Replace($"{value}", x => $"{x.Value[0]} {x.Value[1]}");
    }

    [GeneratedRegex(@"[a-z][A-Z]")]
    private static partial Regex WordRegex();
}

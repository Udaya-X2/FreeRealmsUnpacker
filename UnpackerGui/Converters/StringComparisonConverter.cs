using Avalonia.Data;
using System.Globalization;
using System;
using Avalonia.Data.Converters;

namespace UnpackerGui.Converters;

public class StringComparisonConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        string a when parameter is string b => a == b,
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

using Avalonia.Data;
using System.Globalization;
using System;
using Avalonia.Data.Converters;

namespace UnpackerGui.Converters;

public class BooleanToRotationConverter : IValueConverter
{
    public static readonly BooleanToRotationConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        true => 0,
        false => 180,
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

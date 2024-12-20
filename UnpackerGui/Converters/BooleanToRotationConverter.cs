using Avalonia.Data;
using System.Globalization;
using System;
using Avalonia.Data.Converters;

namespace UnpackerGui.Converters;

public class BooleanToRotationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        bool rotate when int.TryParse(parameter as string, out int angle) => rotate ? angle : 0,
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

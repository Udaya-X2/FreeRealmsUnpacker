using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UnpackerGui.Converters;

public class ExpressionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        int lhs when parameter is string rhs => rhs[0] switch
        {
            '>' => lhs > int.Parse(rhs.AsSpan(1)),
            '<' => lhs < int.Parse(rhs.AsSpan(1)),
            _ => lhs == int.Parse(rhs)
        },
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

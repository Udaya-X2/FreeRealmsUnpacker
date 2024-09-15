using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace UnpackerGui.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public static readonly EnumToBooleanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        Enum when Enum.TryParse(value.GetType(), parameter as string, out object? result) => value.Equals(result),
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        true when Enum.TryParse(targetType, parameter as string, out object? result) => result,
        false => BindingOperations.DoNothing,
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };
}

using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Reflection;

namespace UnpackerGui.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value?.GetType() is Type valueType && Enum.TryParse(valueType, parameter as string, out object? result))
        {
            if (valueType.IsDefined(typeof(FlagsAttribute)))
            {
                return ((int)value & (int)result) == (int)result;
            }
            else
            {
                return value.Equals(result);
            }
        }
        else
        {
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        true when Enum.TryParse(targetType, parameter as string, out object? result) => result,
        false => BindingOperations.DoNothing,
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };
}

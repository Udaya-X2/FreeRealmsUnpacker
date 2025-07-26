using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace UnpackerGui.Converters;

public class EqualityConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => Equals(values[0], values[1]);
}

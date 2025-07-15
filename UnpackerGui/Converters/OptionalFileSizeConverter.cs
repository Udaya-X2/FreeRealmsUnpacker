using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace UnpackerGui.Converters;

public class OptionalFileSizeConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => values[1] switch
        {
            true => values[0]!.ToString(),
            null => FileSizeConverter.GetFileSize((uint)values[0]!),
            _ => null
        };
}

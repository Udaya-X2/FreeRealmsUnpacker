using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media.Immutable;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace UnpackerGui.Converters;

public class ColorOpacityConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
        {
            return new BindingNotification(new ArgumentOutOfRangeException(nameof(values)), BindingErrorType.Error);
        }
        if (values[0] is not DynamicResourceExtension { ResourceKey: object key })
        {
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }
        if (!double.TryParse(values[1] as string, out double opacity))
        {
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }
        if (!App.TryGetResource(key, out ImmutableSolidColorBrush? brush))
        {
            return new BindingNotification(new KeyNotFoundException(), BindingErrorType.Error);
        }

        return new ImmutableSolidColorBrush(brush.Color, opacity);
    }
}

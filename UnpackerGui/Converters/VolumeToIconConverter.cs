using Avalonia.Data;
using Avalonia.Data.Converters;
using FluentIcons.Common;
using System;
using System.Globalization;

namespace UnpackerGui.Converters;

public class VolumeToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        int volume => volume switch
        {
            > 50 => Icon.Speaker2,
            > 0 => Icon.Speaker1,
            _ => Icon.SpeakerMute
        },
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

﻿using Avalonia.Data;
using System.Globalization;
using System;
using Avalonia.Data.Converters;
using FluentIcons.Common;

namespace UnpackerGui.Converters;

public class BooleanToIconConverter : IValueConverter
{
    private const char Separator = '/';

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        bool condition when Split(parameter) is (string val1, string val2) => Enum.Parse<Icon>(condition ? val1 : val2),
        _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;

    private static (string, string)? Split(object? parameter)
    {
        if (parameter is not string values) return null;
        
        string[] splitValues = values.Split(Separator);

        if (splitValues.Length != 2) return null;

        return (splitValues[0], splitValues[1]);
    }
}

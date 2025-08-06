using Avalonia.Markup.Xaml;
using System;

namespace UnpackerGui.Commands;

/// <summary>
/// Allows binding a double value to a command parameter.
/// </summary>
/// <param name="value">A double value.</param>
public class DoubleExtension(double value) : MarkupExtension
{
    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider sp) => value;
}

using Avalonia.Input;
using System;
using System.Reactive.Disposables;

namespace UnpackerGui.Extensions;

/// <summary>
/// Provides extension methods for Avalonia controls.
/// </summary>
public static class ControlExtensions
{
    /// <summary>
    /// Disables the specified control.
    /// </summary>
    /// <param name="inputElement">The control to disable.</param>
    /// <returns>A disposable that, when disposed, enables the control.</returns>
    public static IDisposable Disable(this InputElement inputElement)
    {
        inputElement.IsEnabled = false;
        return Disposable.Create(() => inputElement.IsEnabled = true);
    }
}

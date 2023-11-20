using System;
using UnpackerGui.Services;
using UnpackerGui.ViewModels;
using UnpackerGui.Views;

namespace UnpackerGui.Observers;

/// <summary>
/// A global error handler that displays a message box when exceptions occur.
/// </summary>
public class GlobalErrorHandler : IObserver<Exception>
{
    public static readonly GlobalErrorHandler Instance = new();

    /// <inheritdoc/>
    public void OnNext(Exception error) => ShowError(error);

    /// <inheritdoc/>
    public void OnError(Exception error) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void OnCompleted() => throw new NotImplementedException();

    private static async void ShowError(Exception error)
    {
        ErrorWindow errorWindow = new()
        {
            DataContext = new ErrorViewModel(error, true)
        };
        await App.GetService<IDialogService>().ShowDialog(errorWindow);
    }
}

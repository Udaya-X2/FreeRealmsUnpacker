using ReactiveUI;
using System;
using System.Reactive.Concurrency;
using UnpackerGui.Services;
using UnpackerGui.ViewModels;
using UnpackerGui.Views;

namespace UnpackerGui.Observers;

/// <summary>
/// A global error handler that displays a message box when unhandled errors occur.
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
        await App.GetService<IDialogService>().ShowTerminalDialog(new ErrorWindow()
        {
            DataContext = new ErrorViewModel(error, false)
        });
        RxApp.MainThreadScheduler.Schedule(() => { throw error; });
    }
}

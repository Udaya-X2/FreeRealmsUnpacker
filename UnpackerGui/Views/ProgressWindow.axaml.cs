using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using UnpackerGui.Extensions;
using UnpackerGui.Services;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class ProgressWindow : Window
{
    /// <summary>
    /// Gets whether to automatically close the window after the task is successfully completed.
    /// </summary>
    public bool AutoClose { get; init; }

    private readonly IDialogService _dialogService;
    private readonly CompositeDisposable _cleanUp;

    public ProgressWindow()
    {
        InitializeComponent();
        _dialogService = new DialogService(this);
        _cleanUp = new CompositeDisposable();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProgressViewModel progress) return;

        // Automatically close the window upon successful task completion, if desired.
        if (AutoClose)
        {
            _cleanUp.Add(progress.WhenAnyValue(x => x.Status)
                                 .Where(x => x.IsCompletedSuccessfully())
                                 .Subscribe(_ => Dispatcher.UIThread.Invoke(Close)));
        }

        // Show a terminal error dialog if an exception occurs during the task.
        progress.Command.ThrownExceptions.Subscribe(async x => await _dialogService.ShowErrorDialog(x, terminal: true));

        // Start the progress task.
        _cleanUp.Add(progress.Command.Invoke());
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!IsEnabled || DataContext is not ProgressViewModel progress || progress.Status.IsCompleted()) return;

        // Disable user interaction.
        Closing -= Window_Closing;
        IsEnabled = false;
        e.Cancel = true;

        // Cancel the task.
        _cleanUp.Dispose();

        // Delay closing the window until the task is completed.
        progress.WhenAnyValue(x => x.Status)
                .Where(x => x.IsCompleted())
                .Subscribe(_ => Dispatcher.UIThread.Invoke(Close));
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Reactive.Disposables;
using System;
using UnpackerGui.Services;
using UnpackerGui.ViewModels;
using Avalonia.Threading;
using ReactiveUI;
using UnpackerGui.Commands;
using System.Reactive.Linq;

namespace UnpackerGui.Views;

public partial class ProgressWindow : Window
{
    private readonly IDialogService _dialogService;
    private readonly CompositeDisposable _cleanUp;

    public ProgressWindow()
    {
        InitializeComponent();
        _dialogService = new DialogService(this);
        _cleanUp = new CompositeDisposable
        {
            Disposable.Create(() => Closing -= Window_Closing)
        };
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProgressViewModel progress) return;

        // Show a terminal error dialog if an exception occurs during the operation.
        progress.Command.ThrownExceptions.Subscribe(async x => await _dialogService.ShowErrorDialog(x, terminal: true));
        _cleanUp.Add(progress.Command.Invoke());
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!IsEnabled || DataContext is not ProgressViewModel progress || progress.IsComplete) return;

        // Disable user interaction and delay closing the window until the operation is complete.
        IsEnabled = false;
        e.Cancel = true;
        progress.WhenAnyValue(x => x.IsComplete)
                .Where(x => x)
                .Subscribe(_ => Dispatcher.UIThread.Invoke(Close));

        // Cancel the operation.
        _cleanUp.Dispose();

        // Close the window if the extraction completed during the previous statements.
        if (progress.IsComplete) e.Cancel = false;
    }

    private void Window_Closed(object? sender, EventArgs e) => _cleanUp.Dispose();
}

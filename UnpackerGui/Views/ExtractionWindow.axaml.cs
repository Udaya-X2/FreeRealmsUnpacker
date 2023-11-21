using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using UnpackerGui.Commands;
using UnpackerGui.Services;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class ExtractionWindow : Window
{
    private readonly IDialogService _dialogService;
    private readonly CompositeDisposable _cleanUp;

    public ExtractionWindow()
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
        if (DataContext is not ExtractionViewModel extraction) return;

        // Show a terminal error dialog if an exception occurs during extraction.
        extraction.ExtractAssetsCommand.ThrownExceptions.Subscribe(async x =>
        {
            _cleanUp.Dispose();
            await _dialogService.ShowErrorDialog(x, terminal: true);
        });
        _cleanUp.Add(extraction.ExtractAssetsCommand.Invoke());
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!IsEnabled || DataContext is not ExtractionViewModel extraction || extraction.ExtractionComplete) return;

        // Disable user interaction and delay closing the window until the extraction is completed.
        IsEnabled = false;
        e.Cancel = true;
        extraction.WhenAnyValue(x => x.ExtractionComplete)
                  .Where(x => x)
                  .Subscribe(_ => Dispatcher.UIThread.Invoke(Close));

        // Cancel the current extraction operation.
        extraction.ExtractionMessage = "Stopping extraction...";
        _cleanUp.Dispose();

        // Close the window if the extraction completed during the previous statements.
        if (extraction.ExtractionComplete) e.Cancel = false;
    }

    private void Window_Closed(object? sender, EventArgs e) => _cleanUp.Dispose();
}

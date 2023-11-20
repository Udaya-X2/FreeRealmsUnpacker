using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using UnpackerGui.Observers;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class ExtractionWindow : Window
{
    private IDisposable? _cancelExtraction;

    public ExtractionWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExtractionViewModel extraction) return;

        // TODO: make extraction operation a one-time event
        // TODO: make extraction window the 'owner' so it gets disabled
        _cancelExtraction = extraction.ExtractAssetsCommand.Invoke();
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not ExtractionViewModel extraction || extraction.ExtractionComplete) return;

        // Disable user interaction and delay closing the window until the extraction is completed.
        Closing -= Window_Closing;
        IsEnabled = false;
        e.Cancel = true;
        extraction.WhenAnyValue(x => x.ExtractionComplete)
                  .Where(x => x)
                  .Subscribe(_ => Dispatcher.UIThread.Invoke(Close));

        // Cancel the current extraction operation.
        extraction.ExtractionMessage = "Stopping extraction...";
        _cancelExtraction?.Dispose();

        // Close the window if the extraction completed during the previous statements.
        if (extraction.ExtractionComplete) e.Cancel = false;
    }
}

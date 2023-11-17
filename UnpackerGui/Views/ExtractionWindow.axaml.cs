using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class ExtractionWindow : Window
{
    private readonly CancellationTokenSource _cts;

    private Task _task;

    public ExtractionWindow()
    {
        InitializeComponent();
        _cts = new CancellationTokenSource();
        _task = Task.CompletedTask;
    }

    private async void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ExtractionViewModel extraction) return;

        // TODO: make extraction window the 'owner' so it gets disabled
        // TODO: make sure canceled exception doesn't cause error
        await (_task = Task.Run(() =>
        {
            try
            {
                extraction.ExtractAssets(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Extraction stopped at asset {extraction.AssetIndex}/{extraction.AssetCount}.");
            }
        }, _cts.Token));
    }

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_task.IsCompleted || DataContext is not ExtractionViewModel extraction) return;

        // Disable user interaction and delay closing the window until the extraction is stopped.
        Closing -= Window_Closing;
        IsEnabled = false;
        e.Cancel = true;
        extraction.ExtractionMessage = "Stopping extraction...";
        _cts.Cancel();
        await _task;
        Close();
    }

    private void Window_Closed(object? sender, EventArgs e) => _cts.Dispose();
}

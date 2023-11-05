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
        if (DataContext is ExtractionViewModel extraction)
        {
            _task = Task.Run(() => extraction.ExtractAssets(_cts.Token), _cts.Token);
            await ExtractionTask();
        }
    }

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!_task.IsCompleted && DataContext is ExtractionViewModel extraction)
        {
            // Disable UI interaction and wait for the extraction to stop before closing the window.
            Closing -= Window_Closing;
            IsEnabled = false;
            e.Cancel = true;
            extraction.ExtractionMessage = "Stopping extraction...";
            _cts.Cancel();
            await ExtractionTask();
            Close();
        }
    }

    private void Window_Closed(object? sender, EventArgs e) => _cts.Dispose();

    private async Task ExtractionTask()
    {
        try
        {
            await _task;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Extraction canceled.");
        }
    }
}

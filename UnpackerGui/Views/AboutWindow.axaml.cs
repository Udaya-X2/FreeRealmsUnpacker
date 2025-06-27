using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Reactive.Disposables;

namespace UnpackerGui.Views;

public partial class AboutWindow : Window
{
    private readonly CompositeDisposable _cleanUp;

    public AboutWindow()
    {
        InitializeComponent();
        _cleanUp = [];
    }

    // Add hotkey event handler (workaround for Linux).
    private void Window_Loaded(object? sender, RoutedEventArgs e)
        => _cleanUp.Add(KeyDownEvent.AddClassHandler<Window>(Window_KeyDown));

    private void Window_Unloaded(object? sender, RoutedEventArgs e) => _cleanUp.Dispose();

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                break;
        }
    }
}

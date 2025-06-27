using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Reactive.Disposables;

namespace UnpackerGui.Views;

public partial class ConfirmWindow : Window
{
    /// <summary>
    /// Gets whether the user confirmed the action.
    /// </summary>
    public bool Confirmed { get; private set; }

    private readonly CompositeDisposable _cleanUp;

    public ConfirmWindow()
    {
        InitializeComponent();
        _cleanUp = [];
    }

    // Add hotkey event handler (workaround for Linux).
    private void Window_Loaded(object? sender, RoutedEventArgs e)
        => _cleanUp.Add(KeyDownEvent.AddClassHandler<Window>(Window_KeyDown));

    private void Window_Unloaded(object? sender, RoutedEventArgs e) => _cleanUp.Dispose();

    private void Yes_Button_Click(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void No_Button_Click(object? sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Y:
                Yes_Button_Click(sender, e);
                break;
            case Key.N:
                No_Button_Click(sender, e);
                break;
            case Key.Escape:
                Close();
                break;
        }
    }
}

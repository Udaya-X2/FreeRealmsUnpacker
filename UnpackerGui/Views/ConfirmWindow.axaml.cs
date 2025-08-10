using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace UnpackerGui.Views;

public partial class ConfirmWindow : Window
{
    /// <summary>
    /// Gets whether the user confirmed the action.
    /// </summary>
    public bool Confirmed { get; private set; }

    public ConfirmWindow()
    {
        InitializeComponent();
    }

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
        }
    }

    private void Yes_Button_Click(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void No_Button_Click(object? sender, RoutedEventArgs e) => Close();
}

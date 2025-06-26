using Avalonia.Controls;
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

    private void Yes_Button_Click(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void No_Button_Click(object? sender, RoutedEventArgs e) => Close();
}

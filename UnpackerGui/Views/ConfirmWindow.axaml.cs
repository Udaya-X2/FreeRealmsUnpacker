using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace UnpackerGui.Views;

public partial class ConfirmWindow : Window
{
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

    private void Yes_Button_Click(object? sender, RoutedEventArgs e) => Close(true);

    private void No_Button_Click(object? sender, RoutedEventArgs e) => Close(false);
}

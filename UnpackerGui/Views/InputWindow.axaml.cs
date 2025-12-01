using Avalonia.Controls;
using Avalonia.Interactivity;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class InputWindow : Window
{
    public InputWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InputViewModel input) return;

        inputTextBox.TextChanged += (s, e) => okButton.IsEnabled = input.IsValid(inputTextBox.Text);
        inputTextBox.Text = input.Value;
        inputTextBox.Focus();
    }

    private void OK_Button_Click(object? sender, RoutedEventArgs e) => Close(inputTextBox.Text);

    private void Cancel_Button_Click(object? sender, RoutedEventArgs e) => Close(null);
}

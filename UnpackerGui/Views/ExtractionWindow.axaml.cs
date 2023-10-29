using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class ExtractionWindow : Window
{
    public ExtractionWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ExtractionViewModel extraction)
        {
            await Task.Run(extraction.ExtractAssets);
        }
    }
}

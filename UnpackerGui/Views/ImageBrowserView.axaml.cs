using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UnpackerGui.Views;

public partial class ImageBrowserView : UserControl
{
    public ImageBrowserView()
    {
        InitializeComponent();
    }

    private void Button_Click_ResetZoom(object? sender, RoutedEventArgs e) => zoomBorder.ResetMatrix();

    private void Button_Click_ZoomOut(object? sender, RoutedEventArgs e) => zoomBorder.ZoomOut();

    private void Button_Click_ZoomIn(object? sender, RoutedEventArgs e) => zoomBorder.ZoomIn();
}

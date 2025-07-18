using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

    private void Slider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (e.NewValue != zoomBorder.ZoomX)
        {
            Rect bounds = zoomBorder.Child!.Bounds;
            zoomBorder.Zoom(e.NewValue, bounds.Width / 2, bounds.Height / 2);
        }
    }
}

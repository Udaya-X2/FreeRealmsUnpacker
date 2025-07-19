using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using UnpackerGui.Commands;
using UnpackerGui.Extensions;
using UnpackerGui.Models;

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

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if ((e.Source as Control)?.Parent is not DataGridCell) return;
        if (imageGrid.SelectedItem is not AssetInfo asset) return;

        StaticCommands.OpenAssetCommand.Invoke(asset);
    }

    private void MenuItem_Click_ShowAssetBrowser(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is not MainWindow mainWindow) return;

        mainWindow.assetBrowserTab.IsSelected = true;
        DataGrid assetGrid = mainWindow.assetBrowserView.assetGrid;
        assetGrid.SelectedItem = imageGrid.SelectedItem;
        assetGrid.ScrollIntoView(assetGrid.SelectedItem, null);
    }
}

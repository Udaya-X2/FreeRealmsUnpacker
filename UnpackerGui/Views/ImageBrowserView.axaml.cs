using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using ReactiveUI;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnpackerGui.Commands;
using UnpackerGui.Extensions;
using UnpackerGui.Models;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class ImageBrowserView : UserControl
{
    public ImageBrowserView()
    {
        InitializeComponent();
    }

    private void View_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AssetBrowserViewModel browser) return;

        // SelectedItems is a not a bindable property in DataGrid, so we need to pass
        // it as a reference to the view model to keep track of the selected assets.
        browser.SelectedAssets.Items = assetGrid.SelectedItems;
        assetGrid.SelectionChanged += browser.SelectedAssets.Refresh;

        // Override the default DataGrid copy behavior.
        assetGrid.KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.C, KeyModifiers.Control),
            Command = ReactiveCommand.CreateFromTask(() => CopyAssetsToClipboard(assetGrid.SelectedItems))
        });
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if ((e.Source as Control)?.Parent is not DataGridCell) return;
        if (assetGrid.SelectedItem is not AssetInfo asset) return;

        StaticCommands.OpenAssetCommand.Invoke(asset);
    }

    private void DataGrid_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        assetGrid.SelectedItem = null;
        assetGrid.SelectedItems.Clear();
    }

    private void MenuItem_Click_ShowAssetBrowser(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is not MainWindow mainWindow) return;

        mainWindow.assetBrowserTab.IsSelected = true;
        mainWindow.assetBrowserView.assetGrid.SelectedItem = assetGrid.SelectedItem;
        mainWindow.assetBrowserView.assetGrid.ScrollIntoView(assetGrid.SelectedItem, null);
    }

    private async void MenuItem_Click_CopyDataGridColumn(object? sender, RoutedEventArgs e)
        => await CopyAssetsToClipboard(assetGrid.CollectionView);

    private async Task CopyAssetsToClipboard(IEnumerable assets)
    {
        if (App.Current?.Settings is not SettingsViewModel settings) return;

        StringBuilder sb = new();

        if (settings.CopyColumnHeaders)
        {
            sb.Append((string)assetGrid.Columns[0].Header);
            sb.Append(settings.ClipboardLineSeparator);
        }

        foreach (AssetInfo asset in assets)
        {
            sb.Append(asset.Name);
            sb.Append(settings.ClipboardLineSeparator);
        }

        await App.SetClipboardText(sb.ToString());
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

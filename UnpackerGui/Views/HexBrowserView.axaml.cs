using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaHex.Document;
using ReactiveUI;
using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnpackerGui.Models;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class HexBrowserView : UserControl
{
    public HexBrowserView()
    {
        InitializeComponent();
        hexEditor.Selection.RangeChanged += OnSelectionRangeChanged;
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

    private void MenuItem_Click_ShowImageBrowser(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is not MainWindow mainWindow) return;

        mainWindow.imageBrowserTab.IsSelected = true;
        mainWindow.imageBrowserView.assetGrid.SelectedItem = assetGrid.SelectedItem;
        mainWindow.imageBrowserView.assetGrid.ScrollIntoView(assetGrid.SelectedItem, null);
    }

    private void MenuItem_Click_ShowAudioBrowser(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is not MainWindow mainWindow) return;

        mainWindow.audioBrowserTab.IsSelected = true;
        mainWindow.audioBrowserView.assetGrid.SelectedItem = assetGrid.SelectedItem;
        mainWindow.audioBrowserView.assetGrid.ScrollIntoView(assetGrid.SelectedItem, null);
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

    private async void CopyHexData(object? sender, RoutedEventArgs e) => await hexEditor.Copy();

    private void OnSelectionRangeChanged(object? s, EventArgs e)
    {
        BitRange range = hexEditor.Selection.Range;
        offsetTextBlock.Text = hexEditor.Document != null ? $"Offset: {range.Start.ByteIndex}" : "";
        lengthTextBlock.Text = range.ByteLength > 1 ? $"Length: {range.ByteLength}" : "";
        blockTextBlock.Text = range.ByteLength > 1 ? $"Block: {range.Start.ByteIndex}-{range.End.ByteIndex - 1}" : "";
    }
}

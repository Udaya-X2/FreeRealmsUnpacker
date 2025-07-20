using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using UnpackerGui.Commands;
using UnpackerGui.Extensions;
using UnpackerGui.Models;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class MainView : UserControl
{
    private readonly CompositeDisposable _cleanUp;

    public MainView()
    {
        InitializeComponent();
        _cleanUp = [];
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;

        // SelectedItems is a not a bindable property in DataGrid, so we need to pass 
        // it as a reference to the view model to keep track of the selected assets.
        mainViewModel.SelectedAssets.Items = assetGrid.SelectedItems;
        assetGrid.SelectionChanged += mainViewModel.SelectedAssets.Refresh;
        _cleanUp.Add(Disposable.Create(() => assetGrid.SelectionChanged -= mainViewModel.SelectedAssets.Refresh));

        // Override the default DataGrid copy behavior.
        assetGrid.KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.C, KeyModifiers.Control),
            Command = ReactiveCommand.CreateFromTask(() => CopyAssetsToClipboard(assetGrid.SelectedItems))
        });

        // Add hotkey/drag-and-drop event handlers (workaround for Linux).
        _cleanUp.Add(KeyDownEvent.AddClassHandler<MainWindow>(MainWindow_OnKeyDown));
        _cleanUp.Add(DragDrop.DropEvent.AddClassHandler<ListBox>(ListBox_Drop));

        // If arguments were passed to the application, load them in as asset files.
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { Args: string[] args })
        {
            mainViewModel.AddArgumentFilesCommand.Execute(args);
        }
    }

    private void MainView_Unloaded(object? sender, RoutedEventArgs e) => _cleanUp.Dispose();

    private void MainWindow_OnKeyDown(MainWindow sender, KeyEventArgs e)
    {
        switch (e.KeyModifiers, e.Key)
        {
#if DEBUG
            case (KeyModifiers.Alt, Key.D):
                static void Print(object? x) => Debug.WriteLine($"{DateTime.Now:[yyyy-MM-dd HH:mm:ss,fff]} {x}");
                assetGrid.KeyBindings.ForEach(Print);
                break;
#endif
            case (KeyModifiers.Alt, Key.C):
                matchCaseButton.IsChecked ^= true;
                break;
            case (KeyModifiers.Alt, Key.E):
                useRegexButton.IsChecked ^= true;
                break;
            case (KeyModifiers.Alt, Key.V):
                validateAssetsButton.IsChecked ^= true;
                break;
            case (KeyModifiers.Control, Key.F):
                searchBarTextBox.Focus();
                break;
        }
    }

    private void AssetGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if ((e.Source as Control)?.Parent is not DataGridCell) return;
        if (assetGrid.SelectedItem is not AssetInfo asset) return;

        StaticCommands.OpenAssetCommand.Invoke(asset);
    }

    private void AssetGrid_Sorting(object? sender, DataGridColumnEventArgs e) => assetGrid.SelectedItems.Clear();

    private async void AssetGridRow_ContextMenu_Copy(object? sender, RoutedEventArgs e)
    {
        string? text = (assetGrid.CurrentColumn.GetCellContent(assetGrid.SelectedItem) as TextBlock)?.Text;
        await App.SetClipboardText(text);
    }

    private async void AssetGridRow_ContextMenu_CopyRows(object? sender, RoutedEventArgs e)
        => await CopyAssetsToClipboard(assetGrid.SelectedItems);

    private void ListBox_Drop(ListBox sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;
        if (e.Data.Get(DataFormats.Files) is not IEnumerable<IStorageItem> files) return;

        mainViewModel.AddDragDropFilesCommand.Invoke(files.OfType<IStorageFile>().Select(x => x.Path.LocalPath));
    }

    private void AssetGridColumnHeader_ContextMenu_Hide(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Parent.Parent.Parent: DataGridColumnHeader { Content: string colName } }) return;
        if (App.Current?.Settings is not SettingsViewModel settings) return;

        _ = colName switch
        {
            "Name" => settings.ShowName = false,
            "Offset" => settings.ShowOffset = false,
            "Size" => settings.ShowSize = false,
            "CRC-32" => settings.ShowCrc32 = false,
            "Type" => settings.ShowType = false,
            _ => throw new ArgumentException($"Cannot hide unknown column: \"{colName}\"")
        };
    }

    private async void AssetGridColumnHeader_ContextMenu_Copy(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Parent.Parent.Parent: DataGridColumnHeader { Content: string colName } }) return;

        await CopyAssetsToClipboard(assetGrid.CollectionView, [colName]);
    }

    private async void AssetGrid_Copy(object? sender, RoutedEventArgs e)
        => await CopyAssetsToClipboard(assetGrid.CollectionView);

    private Task CopyAssetsToClipboard(IEnumerable assets)
        => CopyAssetsToClipboard(assets, [.. assetGrid.Columns.Where(x => x.IsVisible)
                                                              .OrderBy(x => x.DisplayIndex)
                                                              .Select(x => (string)x.Header)]);

    private static async Task CopyAssetsToClipboard(IEnumerable assets, IList<string> colNames)
    {
        if (colNames.Count == 0) return;
        if (App.Current?.Settings is not SettingsViewModel settings) return;

        Func<AssetInfo, string>[] selectors = [.. colNames.Select(x => GetAssetInfoSelector(settings, x))];
        string[] values = new string[selectors.Length];
        StringBuilder sb = new();

        if (settings.CopyColumnHeaders)
        {
            sb.AppendJoin(settings.ClipboardSeparator, colNames);
            sb.Append(settings.ClipboardLineSeparator);
        }

        foreach (AssetInfo asset in assets)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = selectors[i](asset);
            }

            sb.AppendJoin(settings.ClipboardSeparator, values);
            sb.Append(settings.ClipboardLineSeparator);
        }

        await App.SetClipboardText(sb.ToString());
    }

    private static Func<AssetInfo, string> GetAssetInfoSelector(SettingsViewModel settings, string? name) => name switch
    {
        "Name" => static x => x.Name,
        "Offset" => static x => x.Offset.ToString(),
        "Size" when settings.ShowSize is null => static x => x.FileSize,
        "Size" => static x => x.Size.ToString(),
        "CRC-32" => static x => x.Crc32.ToString(),
        "Type" => static x => x.Type,
        _ => throw new ArgumentException($"Could not acquire selector for unknown name: \"{name}\"")
    };
}

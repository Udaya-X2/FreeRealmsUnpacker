using AssetIO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using UnpackerGui.Models;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class MainView : UserControl
{
    private readonly CompositeDisposable _cleanUp;

    public MainView()
    {
        InitializeComponent();
        _cleanUp = new CompositeDisposable();
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
        {
            // SelectedItems is a not a bindable property in DataGrid, so we need to pass 
            // it as a reference to the view model to keep track of the selected assets.
            mainViewModel.SelectedAssets.Items = assetGrid.SelectedItems;
            assetGrid.SelectionChanged += mainViewModel.SelectedAssets.Refresh;
            _cleanUp.Add(Disposable.Create(() => assetGrid.SelectionChanged -= mainViewModel.SelectedAssets.Refresh));
        }

        _cleanUp.Add(KeyDownEvent.AddClassHandler<MainWindow>(MainWindow_OnKeyDown));
        _cleanUp.Add(DragDrop.DropEvent.AddClassHandler<TreeView>(TreeView_Drop));
    }

    private void MainView_Unloaded(object? sender, RoutedEventArgs e) => _cleanUp.Dispose();

    private void AssetGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (assetGrid.SelectedItem is not AssetInfo asset) return;

        // Extract the double-clicked asset to a temporary location and open it.
        string tempDir = Path.GetTempPath();
        FileInfo file = new(Path.Combine(tempDir, Guid.NewGuid().ToString(), asset.Name));
        using AssetReader reader = asset.AssetFile.OpenRead();
        file.Directory?.Create();
        using FileStream fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
        reader.CopyTo(asset, fs);
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = file.FullName
        });
    }

    private void MainWindow_OnKeyDown(MainWindow sender, KeyEventArgs e)
    {
        switch (e.KeyModifiers, e.Key)
        {
            case (KeyModifiers.Alt, Key.C):
                matchCaseButton.IsChecked ^= true;
                break;
            case (KeyModifiers.Alt, Key.E):
                useRegexButton.IsChecked ^= true;
                break;
            case (KeyModifiers.Control, Key.F):
                searchBarTextBox.Focus();
                break;
        }
    }

    private void TreeView_Drop(TreeView sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;
        if (e.Data.Get(DataFormats.Files) is not IEnumerable<IStorageItem> files) return;

        mainViewModel.AddFiles(files.OfType<IStorageFile>().Select(x => x.Path.LocalPath));
    }
}

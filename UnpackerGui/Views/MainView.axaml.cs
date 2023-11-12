using AssetIO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using UnpackerGui.Models;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class MainView : UserControl
{
    private readonly List<IDisposable> _disposables;

    public MainView()
    {
        InitializeComponent();
        _disposables = new List<IDisposable>();
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
        {
            // SelectedItems is a not a bindable property in DataGrid, so we need to pass 
            // it as a reference to the view model to keep track of the selected assets.
            mainViewModel.SelectedAssets.Items = assetGrid.SelectedItems;
            assetGrid.SelectionChanged += mainViewModel.SelectedAssets.OnCollectionChanged;
            _disposables.Add(Disposable.Create(() =>
            {
                assetGrid.SelectionChanged -= mainViewModel.SelectedAssets.OnCollectionChanged;
            }));
        }

        // Handle key events from the main window in this view.
        _disposables.Add(KeyDownEvent.AddClassHandler<MainWindow>(OnKeyDown));
    }

    private void MainView_Unloaded(object? sender, RoutedEventArgs e)
    {
        _disposables.ForEach(x => x.Dispose());
        _disposables.Clear();
    }

    private void AssetGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (assetGrid.SelectedItem is AssetInfo asset)
        {
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
    }

    private void OnKeyDown(MainWindow sender, KeyEventArgs e)
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
}

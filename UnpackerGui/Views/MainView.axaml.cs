using AssetIO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using UnpackerGui.Models;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.SelectedAssets.Items = assetGrid.SelectedItems;
            assetGrid.SelectionChanged += mainViewModel.SelectedAssets.OnCollectionChanged;
        }
    }

    private void AssetGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (assetGrid.SelectedItem is AssetInfo asset)
        {
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
}

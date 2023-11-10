using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Diagnostics;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.LeftAlt && DataContext is MainViewModel data)
            {
                Debug.WriteLine(new string('-', 80));
                Debug.WriteLine($"[{DateTime.Now}] AssetFiles.Count = {data.AssetFiles.Count}");
                Debug.WriteLine($"[{DateTime.Now}] Assets.Count = {data.Assets.Count}");
                Debug.WriteLine($"[{DateTime.Now}] SelectedAssetFile = {data.SelectedAssetFile?.GetType()}");
                Debug.WriteLine($"[{DateTime.Now}] CheckedAssetFiles.Count = {data.CheckedAssetFiles.Count}");
                Debug.WriteLine(new string('-', 80));
            }
        };
#endif
    }
}

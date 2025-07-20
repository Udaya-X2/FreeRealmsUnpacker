using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;
using System;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (App.Current?.Settings is not SettingsViewModel settings) return;

        // Change the view to the asset browser if the current view is hidden.
        settings.WhenAnyValue(x => x.ShowImageBrowser)
                .Subscribe(isVisible =>
                {
                    if (!isVisible && imageBrowserTab.IsSelected)
                    {
                        assetBrowserTab.IsSelected = true;
                    }
                });
    }
}

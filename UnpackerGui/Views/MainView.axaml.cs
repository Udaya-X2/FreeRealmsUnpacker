using Avalonia.Controls;
using Avalonia.Interactivity;
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
}

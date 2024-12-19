using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using UnpackerGui.Extensions;
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
        if (DataContext is MainViewModel mainViewModel)
        {
            // SelectedItems is a not a bindable property in DataGrid, so we need to pass 
            // it as a reference to the view model to keep track of the selected assets.
            mainViewModel.SelectedAssets.Items = assetGrid.SelectedItems;
            assetGrid.SelectionChanged += mainViewModel.SelectedAssets.Refresh;
            _cleanUp.Add(Disposable.Create(() => assetGrid.SelectionChanged -= mainViewModel.SelectedAssets.Refresh));
        }

        _cleanUp.Add(KeyDownEvent.AddClassHandler<MainWindow>(MainWindow_OnKeyDown));
        _cleanUp.Add(DragDrop.DropEvent.AddClassHandler<ListBox>(ListBox_Drop));
    }

    private void MainView_Unloaded(object? sender, RoutedEventArgs e) => _cleanUp.Dispose();

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
        if (DataContext is not MainViewModel mainViewModel) return;
        if ((e.Source as Control)?.Parent is not DataGridCell) return;

        mainViewModel.OpenSelectedAssetCommand.Invoke();
    }

    private void AssetGrid_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;

        mainViewModel.ClearSelectedAssetsCommand.Invoke();
    }

    private void AssetGrid_ContextMenu_Copy(object? sender, RoutedEventArgs e)
        => App.SetClipboardText((assetGrid.CurrentColumn.GetCellContent(assetGrid.SelectedItem) as TextBlock)?.Text);

    private void ListBox_Drop(ListBox sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;
        if (e.Data.Get(DataFormats.Files) is not IEnumerable<IStorageItem> files) return;

        mainViewModel.AddFilesCommand.Invoke(files.OfType<IStorageFile>().Select(x => x.Path.LocalPath));
    }
}

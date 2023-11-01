using AssetIO;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using UnpackerGui.Collections;
using UnpackerGui.Models;
using UnpackerGui.Services;
using UnpackerGui.Storage;
using UnpackerGui.Views;

namespace UnpackerGui.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ReactiveList<AssetInfo> Assets { get; }
    public ReactiveList<AssetInfo> SelectedAssets { get; }
    public ReactiveList<AssetFileViewModel> AssetFiles { get; }

    public ICommand AddPackFilesCommand { get; }
    public ICommand AddManifestFilesCommand { get; }
    public ICommand ExtractFilesCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand RemoveSelectedCommand { get; }

    public MainViewModel()
    {
        Assets = new ReactiveList<AssetInfo>();
        SelectedAssets = new ReactiveList<AssetInfo>();
        AssetFiles = new ReactiveList<AssetFileViewModel>();

        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        AddManifestFilesCommand = ReactiveCommand.CreateFromTask(AddManifestFiles);
        ExtractFilesCommand = ReactiveCommand.CreateFromTask(ExtractFiles);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
        RemoveSelectedCommand = ReactiveCommand.Create(RemoveSelected);
    }

    private async Task AddPackFiles()
        => await AddAssetFiles(AssetType.Game | AssetType.Pack, FileTypeFilters.PackFiles);

    private async Task AddManifestFiles()
        => await AddAssetFiles(AssetType.Game | AssetType.Dat, FileTypeFilters.DatFiles);

    private async Task AddAssetFiles(AssetType assetType, FilePickerFileType[] fileTypeFilter)
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = fileTypeFilter
        });

        foreach (IStorageFile file in files)
        {
            string path = file.Path.LocalPath;

            if (AssetFiles.All(x => x.FullName != path))
            {
                AssetFileViewModel assetFile = new(path, assetType);
                assetFile.PropertyChanged += UpdateAssets;
                AssetFiles.Add(assetFile);
            }
        }
    }

    private void UpdateAssets(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(AssetFileViewModel.IsChecked)) return;
        if (sender is not AssetFileViewModel assetFile) return;

        if (assetFile.IsChecked)
        {
            Assets.AddRange(assetFile.Assets);
        }
        else if (Assets.Count > 0)
        {
            Assets.RemoveAll(assetFile.Contains);
        }
    }

    private async Task ExtractFiles()
    {
        if (!AssetFiles.Any(x => x.IsChecked)) return;
        if (await App.GetService<IFilesService>().OpenFolderAsync() is not IStorageFolder folder) return;

        ExtractionWindow extractionWindow = new()
        {
            DataContext = new ExtractionViewModel(folder.Path.LocalPath, AssetFiles)
        };
        IDialogService dialogService = App.GetService<IDialogService>();
        await dialogService.ShowDialog(extractionWindow);
    }

    private void SelectAll()
    {
        using (Assets.SuspendNotifications())
        {
            AssetFiles.ForEach(x => x.IsChecked = true);
        }
    }

    private void DeselectAll()
    {
        Assets.Clear();
        AssetFiles.ForEach(x => x.IsChecked = false);
    }

    private void RemoveSelected()
    {
        Assets.Clear();
        int index = 0;

        foreach (AssetFileViewModel assetFile in AssetFiles.ToArray())
        {
            if (assetFile.IsChecked)
            {
                assetFile.IsChecked = false;
                AssetFiles.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }
}

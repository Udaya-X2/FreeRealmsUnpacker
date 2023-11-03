using AssetIO;
using Avalonia.Platform.Storage;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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
    public ICommand AddDataFilesCommand { get; }
    public ICommand ExtractFilesCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand RemoveSelectedCommand { get; }

    private AssetFileViewModel? _selectedAssetFile;
    private bool _manifestFileSelected;

    public MainViewModel()
    {
        Assets = new ReactiveList<AssetInfo>();
        SelectedAssets = new ReactiveList<AssetInfo>();
        AssetFiles = new ReactiveList<AssetFileViewModel>();

        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        AddManifestFilesCommand = ReactiveCommand.CreateFromTask(AddManifestFiles);
        AddDataFilesCommand = ReactiveCommand.CreateFromTask(AddDataFiles);
        ExtractFilesCommand = ReactiveCommand.CreateFromTask(ExtractFiles);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
        RemoveSelectedCommand = ReactiveCommand.Create(RemoveSelected);

        this.WhenAnyValue(x => x.SelectedAssetFile)
            .Select(x => x?.FileType is AssetType.Dat)
            .BindTo(this, x => x.ManifestFileSelected);
    }

    public AssetFileViewModel? SelectedAssetFile
    {
        get => _selectedAssetFile;
        set => this.RaiseAndSetIfChanged(ref _selectedAssetFile, value);
    }

    public bool ManifestFileSelected
    {
        get => _manifestFileSelected;
        set => this.RaiseAndSetIfChanged(ref _manifestFileSelected, value);
    }

    private async Task AddPackFiles()
        => await AddAssetFiles(AssetType.Game | AssetType.Pack, FileTypeFilters.PackFiles);

    private async Task AddManifestFiles()
        => await AddAssetFiles(AssetType.Game | AssetType.Dat, FileTypeFilters.ManifestFiles);

    private async Task AddAssetFiles(AssetType assetType, FilePickerFileType[] fileTypeFilter)
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = fileTypeFilter
        });
        IEnumerable<string> assetFiles = files.Select(x => x.Path.LocalPath)
                                              .Except(AssetFiles.Select(x => x.FullName));

        foreach (string path in assetFiles)
        {
            AssetFileViewModel assetFile = new(path, assetType);
            assetFile.WhenAnyValue(x => x.IsChecked)
                     .Subscribe(_ => UpdateAssets(assetFile));
            AssetFiles.Add(assetFile);
        }
    }

    public async Task AddDataFiles()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = FileTypeFilters.AssetDatFiles
        });
        ReactiveList<string>? dataFiles = SelectedAssetFile?.DataFilePaths;
        dataFiles?.AddRange(files.Select(x => x.Path.LocalPath).Except(dataFiles));
    }

    private void UpdateAssets(AssetFileViewModel assetFile)
    {
        if (assetFile.IsChecked)
        {
            Assets.AddRange(assetFile.Assets);
        }
        else
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
            DataContext = new ExtractionViewModel(folder.Path.LocalPath, AssetFiles.Where(x => x.IsChecked))
        };
        IDialogService dialogService = App.GetService<IDialogService>();
        await dialogService.ShowDialog(extractionWindow);
    }

    private void SelectAll()
    {
        if (ManifestFileSelected)
        {
            SelectedAssetFile?.DataFiles?.ForEach(x => x.IsChecked = true);
        }
        else
        {
            using (Assets.SuspendNotifications())
            {
                AssetFiles.ForEach(x => x.IsChecked = true);
            }
        }
    }

    private void DeselectAll()
    {
        if (ManifestFileSelected)
        {
            SelectedAssetFile?.DataFiles?.ForEach(x => x.IsChecked = false);
        }
        else
        {
            Assets.Clear();
            AssetFiles.ForEach(x => x.IsChecked = false);
        }
    }

    private void RemoveSelected()
    {
        if (ManifestFileSelected)
        {
            SelectedAssetFile?.DataFilePaths.RemoveMany(SelectedAssetFile!.DataFiles!.Where(x => x.IsChecked)
                                                                                     .Select(x => x.FullName)
                                                                                     .ToList());
        }
        else
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
}

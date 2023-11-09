using AssetIO;
using Avalonia;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Aggregation;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public ControlledObservableList SelectedAssets { get; }
    public GroupedReactiveCollection<AssetFileViewModel, AssetInfo> Assets { get; }

    public ICommand AddPackFilesCommand { get; }
    public ICommand AddManifestFilesCommand { get; }
    public ICommand AddDataFilesCommand { get; }
    public ICommand ExtractFilesCommand { get; }
    public ICommand ExtractAssetsCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand RemoveSelectedCommand { get; }

    private readonly SourceList<AssetFileViewModel> _sourceAssetFiles;
    private readonly ReadOnlyObservableCollection<AssetFileViewModel> _assetFiles;
    private readonly ReadOnlyObservableCollection<AssetFileViewModel> _checkedAssetFiles;

    private int _numAssets;
    private int _numCheckedAssets;
    private AssetFileViewModel? _selectedAssetFile;
    private bool _manifestFileSelected;
    private string _searchText;

    public MainViewModel()
    {
        // Initialize each command.
        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        AddManifestFilesCommand = ReactiveCommand.CreateFromTask(AddManifestFiles);
        AddDataFilesCommand = ReactiveCommand.CreateFromTask(AddDataFiles);
        ExtractFilesCommand = ReactiveCommand.CreateFromTask(ExtractFiles);
        ExtractAssetsCommand = ReactiveCommand.CreateFromTask(ExtractAssets);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
        RemoveSelectedCommand = ReactiveCommand.Create(RemoveSelected);

        // Observe any changes in the asset files.
        _sourceAssetFiles = new SourceList<AssetFileViewModel>();
        _searchText = "";
        var source = _sourceAssetFiles.Connect();

        // Update asset files when changed.
        source.Bind(out _assetFiles)
              // Update checked asset files when checked.
              .AutoRefresh(x => x.IsChecked)
              .Filter(x => x.IsChecked)
              .Bind(out _checkedAssetFiles)
              // Refresh assets shown & update checked asset count.
              .Subscribe(_ =>
              {
                  Assets?.Refresh();
                  NumCheckedAssets = CheckedAssetFiles.Sum(x => x.Count);
              });

        // Update total asset count when asset files change.
        source.ForAggregation()
              .Sum(x => x.Count)
              .BindTo(this, x => x.NumAssets);

        // Keep track of whether selected asset file is a manifest file.
        this.WhenAnyValue(x => x.SelectedAssetFile)
            .Select(x => x?.FileType is AssetType.Dat)
            .BindTo(this, x => x.ManifestFileSelected);

        // Initialize each observable collection.
        SelectedAssets = new ControlledObservableList();
        Assets = new GroupedReactiveCollection<AssetFileViewModel, AssetInfo>(CheckedAssetFiles);
    }

    public ReadOnlyObservableCollection<AssetFileViewModel> AssetFiles => _assetFiles;

    public ReadOnlyObservableCollection<AssetFileViewModel> CheckedAssetFiles => _checkedAssetFiles;

    public int NumAssets
    {
        get => _numAssets;
        set => this.RaiseAndSetIfChanged(ref _numAssets, value);
    }

    public int NumCheckedAssets
    {
        get => _numCheckedAssets;
        set => this.RaiseAndSetIfChanged(ref _numCheckedAssets, value);
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

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
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
        _sourceAssetFiles.AddRange(files.Select(x => x.Path.LocalPath)
                                        .Except(AssetFiles.Select(x => x.FullName))
                                        .Select(x => new AssetFileViewModel(x, assetType)));
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

    private async Task ExtractFiles()
    {
        if (CheckedAssetFiles.Count == 0) return;
        if (await App.GetService<IFilesService>().OpenFolderAsync() is not IStorageFolder folder) return;

        ExtractionWindow extractionWindow = new()
        {
            DataContext = new ExtractionViewModel(folder.Path.LocalPath, CheckedAssetFiles)
        };
        IDialogService dialogService = App.GetService<IDialogService>();
        await dialogService.ShowDialog(extractionWindow);
    }

    private async Task ExtractAssets()
    {
        if (SelectedAssets.Count == 0) return;
        if (await App.GetService<IFilesService>().OpenFolderAsync() is not IStorageFolder folder) return;

        ExtractionWindow extractionWindow = new()
        {
            DataContext = new ExtractionViewModel(folder.Path.LocalPath,
                                                  SelectedAssets.Cast<AssetInfo>(),
                                                  SelectedAssets.Count)
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
            using IDisposable _ = Assets.SuspendNotifications();
            AssetFiles.ForEach(x => x.IsChecked = true);
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
            using IDisposable _ = Assets.SuspendNotifications();
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
            using IDisposable _ = Assets.SuspendNotifications();
            _sourceAssetFiles.RemoveMany(CheckedAssetFiles.ToArray());
        }
    }
}

using AssetIO;
using Avalonia;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using UnpackerGui.Collections;
using UnpackerGui.Extensions;
using UnpackerGui.Models;
using UnpackerGui.Services;
using UnpackerGui.Storage;
using UnpackerGui.Views;

namespace UnpackerGui.ViewModels;

public class MainViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the selected assets.
    /// </summary>
    public ControlledObservableList SelectedAssets { get; }

    /// <summary>
    /// Gets the assets shown to the user.
    /// </summary>
    public FilteredReactiveCollection<AssetInfo> Assets { get; }

    /// <summary>
    /// Gets the assets in the checked asset files.
    /// </summary>
    public ReadOnlyReactiveCollection<AssetInfo> CheckedAssets { get; }

    /// <summary>
    /// Gets the validated assets in the checked asset files.
    /// </summary>
    public ReadOnlyReactiveCollection<AssetInfo> ValidatedAssets { get; }

    /// <summary>
    /// Gets the search options to filter the assets shown.
    /// </summary>
    public SearchOptionsViewModel<AssetInfo> SearchOptions { get; }

    /// <summary>
    /// Gets the validation options to filter the assets shown.
    /// </summary>
    public ValidationOptionsViewModel<AssetInfo> ValidationOptions { get; }

    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowPreferencesCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
    public ReactiveCommand<Unit, Unit> AddPackFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddManifestFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddDataFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractAssetsCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleValidationCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckAllCommand { get; }
    public ReactiveCommand<Unit, Unit> UncheckAllCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveCheckedCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSelectedAssetCommand { get; }
    public ReactiveCommand<IEnumerable<string>, Unit> AddFilesCommand { get; }

    private readonly SourceList<AssetFileViewModel> _sourceAssetFiles;
    private readonly ReadOnlyObservableCollection<AssetFileViewModel> _assetFiles;
    private readonly ReadOnlyObservableCollection<AssetFileViewModel> _checkedAssetFiles;

    private int _numAssets;
    private AssetFileViewModel? _selectedAssetFile;
    private AssetInfo? _selectedAsset;
    private IDisposable? _validationHandler;
    private bool _isValidatingAssets;
    private bool _manifestFileSelected;
    private bool _showName;
    private bool _showOffset;
    private bool _showSize;
    private bool _showCrc32;
    private FileConflictOptions _conflictOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
    {
        // Initialize each command.
        ExitCommand = ReactiveCommand.Create(App.ShutDown);
        ShowPreferencesCommand = ReactiveCommand.CreateFromTask(ShowPreferences);
        ShowAboutCommand = ReactiveCommand.CreateFromTask(ShowAbout);
        OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenFolder);
        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFile);
        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        AddManifestFilesCommand = ReactiveCommand.CreateFromTask(AddManifestFiles);
        AddDataFilesCommand = ReactiveCommand.CreateFromTask(AddDataFiles);
        ExtractFilesCommand = ReactiveCommand.CreateFromTask(ExtractFiles);
        ExtractAssetsCommand = ReactiveCommand.CreateFromTask(ExtractAssets);
        ToggleValidationCommand = ReactiveCommand.CreateFromTask(ToggleValidation);
        CheckAllCommand = ReactiveCommand.Create(CheckAll);
        UncheckAllCommand = ReactiveCommand.Create(UncheckAll);
        RemoveCheckedCommand = ReactiveCommand.Create(RemoveChecked);
        OpenSelectedAssetCommand = ReactiveCommand.Create(OpenSelectedAsset);
        AddFilesCommand = ReactiveCommand.CreateFromTask<IEnumerable<string>>(AddFiles);

        // Observe any changes in the asset files.
        _sourceAssetFiles = new SourceList<AssetFileViewModel>();
        var source = _sourceAssetFiles.Connect();

        // Update asset files when changed.
        source.Bind(out _assetFiles)
              // Update checked asset files when checked.
              .AutoRefresh(x => x.IsChecked)
              .Filter(x => x.IsChecked)
              .Bind(out _checkedAssetFiles)
              .Subscribe();

        // Update total asset count when asset files change.
        source.ForAggregation()
              .Sum(x => x.Count)
              .BindTo(this, x => x.NumAssets);

        // Initialize each observable collection.
        ValidationOptions = new ValidationOptionsViewModel<AssetInfo>(x => x.IsValid);
        SearchOptions = new SearchOptionsViewModel<AssetInfo>(x => x.Name);
        SelectedAssets = new ControlledObservableList();
        CheckedAssets = CheckedAssetFiles.Flatten<ReadOnlyObservableCollection<AssetFileViewModel>, AssetInfo>();
        ValidatedAssets = CheckedAssets.Filter(ValidationOptions);
        Assets = ValidatedAssets.Filter(SearchOptions);

        // Toggle asset validation when requested.
        this.WhenAnyValue(x => x.IsValidatingAssets)
            .Subscribe(_ => ToggleValidationCommand.Invoke());

        // Keep track of whether selected asset file is a manifest file.
        this.WhenAnyValue(x => x.SelectedAssetFile)
            .Select(x => x?.FileType is AssetType.Dat)
            .BindTo(this, x => x.ManifestFileSelected);

        // Need to clear selected assets to avoid the UI freezing when a large
        // number of assets are selected while more assets are added/removed.
        Assets.ObserveCollectionChanges()
              .Subscribe(_ =>
              {
                  SelectedAsset = null;
                  SelectedAssets.Clear();
              });

        // Show each asset property by default.
        _showName = true;
        _showOffset = true;
        _showSize = true;
        _showCrc32 = true;
    }

    /// <summary>
    /// Gets the asset files.
    /// </summary>
    public ReadOnlyObservableCollection<AssetFileViewModel> AssetFiles => _assetFiles;

    /// <summary>
    /// Gets the checked asset files.
    /// </summary>
    public ReadOnlyObservableCollection<AssetFileViewModel> CheckedAssetFiles => _checkedAssetFiles;

    /// <summary>
    /// Gets or sets the total number of assets.
    /// </summary>
    public int NumAssets
    {
        get => _numAssets;
        set => this.RaiseAndSetIfChanged(ref _numAssets, value);
    }

    /// <summary>
    /// Gets or sets the selected asset file.
    /// </summary>
    public AssetFileViewModel? SelectedAssetFile
    {
        get => _selectedAssetFile;
        set => this.RaiseAndSetIfChanged(ref _selectedAssetFile, value);
    }

    /// <summary>
    /// Gets or sets the selected asset.
    /// </summary>
    public AssetInfo? SelectedAsset
    {
        get => _selectedAsset;
        set => this.RaiseAndSetIfChanged(ref _selectedAsset, value);
    }

    /// <summary>
    /// Gets or sets whether to validate assets in checked files.
    /// </summary>
    public bool IsValidatingAssets
    {
        get => _isValidatingAssets;
        set => this.RaiseAndSetIfChanged(ref _isValidatingAssets, value);
    }

    /// <summary>
    /// Gets or sets whether a manifest.dat file is selected.
    /// </summary>
    public bool ManifestFileSelected
    {
        get => _manifestFileSelected;
        set => this.RaiseAndSetIfChanged(ref _manifestFileSelected, value);
    }

    /// <summary>
    /// Gets or sets whether to show the asset's name.
    /// </summary>
    public bool ShowName
    {
        get => _showName;
        set => this.RaiseAndSetIfChanged(ref _showName, value);
    }

    /// <summary>
    /// Gets or sets whether to show the asset's offset.
    /// </summary>
    public bool ShowOffset
    {
        get => _showOffset;
        set => this.RaiseAndSetIfChanged(ref _showOffset, value);
    }

    /// <summary>
    /// Gets or sets whether to show the asset's size.
    /// </summary>
    public bool ShowSize
    {
        get => _showSize;
        set => this.RaiseAndSetIfChanged(ref _showSize, value);
    }

    /// <summary>
    /// Gets or sets whether to show the asset's CRC-32.
    /// </summary>
    public bool ShowCrc32
    {
        get => _showCrc32;
        set => this.RaiseAndSetIfChanged(ref _showCrc32, value);
    }

    /// <summary>
    /// Gets or sets how to handle assets with conflicting names.
    /// </summary>
    public FileConflictOptions ConflictOptions
    {
        get => _conflictOptions;
        set => this.RaiseAndSetIfChanged(ref _conflictOptions, value);
    }

    /// <summary>
    /// Opens the Preferences window.
    /// </summary>
    private async Task ShowPreferences() => await App.GetService<IDialogService>().ShowDialog(new PreferencesWindow
    {
        DataContext = new PreferencesViewModel(this)
    });

    /// <summary>
    /// Opens the About Free Realms Unpacker window.
    /// </summary>
    private async Task ShowAbout() => await App.GetService<IDialogService>().ShowDialog(new AboutWindow
    {
        DataContext = new AboutViewModel()
    });

    /// <summary>
    /// Opens a folder dialog that allows the user to add asset files in the folder to the source asset files.
    /// </summary>
    private async Task OpenFolder()
    {
        if (await App.GetService<IFilesService>().OpenFolderAsync() is not IStorageFolder folder) return;

        List<AssetFile> assetFiles = ClientDirectory.EnumerateAssetFiles(folder.Path.LocalPath)
                                                    .ExceptBy(AssetFiles.Select(x => x.FullName), x => x.FullName)
                                                    .ToList();

        if (assetFiles.Count > 0)
        {
            await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
            {
                DataContext = new ReaderViewModel(_sourceAssetFiles, assetFiles),
                AutoClose = true
            });
        }
    }

    /// <summary>
    /// Opens a file dialog that allows the user to add .pack or manifest.dat files to the source asset files.
    /// </summary>
    private async Task OpenFile()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = FileTypeFilters.AssetFiles
        });
        List<AssetFile> assetFiles = files.Select(x => x.Path.LocalPath)
                                          .Except(AssetFiles.Select(x => x.FullName))
                                          .Select(x => new AssetFile(x))
                                          .ToList();

        if (assetFiles.Count > 0)
        {
            await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
            {
                DataContext = new ReaderViewModel(_sourceAssetFiles, assetFiles),
                AutoClose = true
            });
        }
    }

    /// <summary>
    /// Opens a file dialog that allows the user to add .pack files to the source asset files.
    /// </summary>
    private async Task AddPackFiles()
        => await AddAssetFiles(AssetType.Pack, FileTypeFilters.PackFiles);

    /// <summary>
    /// Opens a file dialog that allows the user to add manifest.dat files to the source asset files.
    /// </summary>
    private async Task AddManifestFiles()
        => await AddAssetFiles(AssetType.Dat, FileTypeFilters.ManifestFiles);

    /// <summary>
    /// Opens a file dialog that allows the user to add asset files of the specified type to the source asset files.
    /// </summary>
    private async Task AddAssetFiles(AssetType assetType, FilePickerFileType[] fileTypeFilter)
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = fileTypeFilter
        });
        List<AssetFile> assetFiles = files.Select(x => x.Path.LocalPath)
                                          .Except(AssetFiles.Select(x => x.FullName))
                                          .Select(x => new AssetFile(x, assetType))
                                          .ToList();

        if (assetFiles.Count > 0)
        {
            await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
            {
                DataContext = new ReaderViewModel(_sourceAssetFiles, assetFiles),
                AutoClose = true
            });
        }
    }

    /// <summary>
    /// Opens a file dialog that allows the user to add asset .dat files to the selected manifest.dat file.
    /// </summary>
    private async Task AddDataFiles()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = FileTypeFilters.AssetDatFiles
        });
        ReactiveList<string>? dataFiles = SelectedAssetFile?.DataFilePaths;
        dataFiles?.AddRange(files.Select(x => x.Path.LocalPath)
                                 .Except(dataFiles)
                                 .ToList());
    }

    /// <summary>
    /// Opens a folder dialog that allows the user to extract the selected asset files to a directory.
    /// </summary>
    private async Task ExtractFiles()
    {
        if (CheckedAssetFiles.Count == 0) return;
        if (await App.GetService<IFilesService>().OpenFolderAsync() is not IStorageFolder folder) return;

        await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
        {
            DataContext = new ExtractionViewModel(folder.Path.LocalPath, CheckedAssetFiles, _conflictOptions)
        });
    }

    /// <summary>
    /// Opens a folder dialog that allows the user to extract the selected assets to a directory.
    /// </summary>
    private async Task ExtractAssets()
    {
        if (SelectedAssets.Count == 0) return;
        if (await App.GetService<IFilesService>().OpenFolderAsync() is not IStorageFolder folder) return;

        await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
        {
            DataContext = new ExtractionViewModel(folder.Path.LocalPath,
                                                  SelectedAssets.Cast<AssetInfo>(),
                                                  SelectedAssets.Count,
                                                  _conflictOptions)
        });
    }

    /// <summary>
    /// Toggles whether to validate assets in checked asset files.
    /// </summary>
    private async Task ToggleValidation()
    {
        if (IsValidatingAssets)
        {
            using (ValidatedAssets.SuspendNotifications())
            {
                IEnumerable<AssetFileViewModel> unvalidatedAssetFiles = CheckedAssetFiles.Where(x => !x.IsValidated);

                // Validate checked asset files that have not been validated yet.
                if (unvalidatedAssetFiles.Any())
                {
                    ValidationViewModel validation = new(unvalidatedAssetFiles);
                    await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
                    {
                        DataContext = validation,
                        AutoClose = true
                    });

                    // If the validation task ended abruptly, uncheck the remaining unvalidated asset files.
                    if (!validation.Status.IsCompletedSuccessfully())
                    {
                        AssetFiles.Where(x => !x.IsValidated)
                                  .ForEach(x => x.IsChecked = false);
                    }

                    ValidatedAssets.Refresh();
                }
                // Ensure future checked asset files are validated before shown.
                if (_validationHandler == null)
                {
                    ValidationOptions.ShowValid = false;
                    _validationHandler = Assets.ObserveCollectionChanges()
                                               .Subscribe(x => ToggleValidationCommand.Invoke());
                }
            }
        }
        else
        {
            // Unsubscribe from the validation handler and reset the assets shown.
            _validationHandler?.Dispose();
            _validationHandler = null;
            ValidationOptions.ShowValid = null;
        }
    }

    /// <summary>
    /// Checks all asset files, or checks the data files under the selected manifest.dat file.
    /// </summary>
    private void CheckAll()
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

    /// <summary>
    /// Unchecks all asset files, or unchecks the data files under the selected manifest.dat file.
    /// </summary>
    private void UncheckAll()
    {
        if (ManifestFileSelected)
        {
            SelectedAssetFile?.DataFiles?.ForEach(x => x.IsChecked = false);
        }
        else
        {
            using (Assets.SuspendNotifications())
            {
                AssetFiles.ForEach(x => x.IsChecked = false);
            }
        }
    }

    /// <summary>
    /// Removes all checked asset files, or removes the checked data files under the selected manifest.dat file.
    /// </summary>
    private void RemoveChecked()
    {
        if (ManifestFileSelected)
        {
            SelectedAssetFile?.DataFilePaths?.RemoveMany(SelectedAssetFile!.DataFiles!.Where(x => x.IsChecked)
                                                                                      .Select(x => x.FullName)
                                                                                      .ToList());
        }
        else
        {
            using (Assets.SuspendNotifications())
            {
                _sourceAssetFiles.RemoveMany(CheckedAssetFiles.ToList());
            }
        }
    }

    /// <summary>
    /// Extracts the selected asset to a temporary location and opens it.
    /// </summary>
    private void OpenSelectedAsset()
    {
        if (SelectedAsset == null) throw new NullReferenceException(nameof(SelectedAsset));

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        using AssetReader reader = SelectedAsset.AssetFile.OpenRead();
        FileInfo file = reader.ExtractTo(SelectedAsset, tempDir);
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = file.FullName
        });
    }

    /// <summary>
    /// Adds the specified files to either the selected manifest.dat file or source asset files.
    /// </summary>
    private async Task AddFiles(IEnumerable<string> files)
    {
        if (files == null) throw new ArgumentNullException(nameof(files));

        if (ManifestFileSelected)
        {
            ReactiveList<string>? dataFiles = SelectedAssetFile?.DataFilePaths;
            dataFiles?.AddRange(files.Except(dataFiles).ToList());
        }
        else
        {
            List<AssetFile> assetFiles = files.Except(AssetFiles.Select(x => x.FullName))
                                              .Select(x =>
                                              {
                                                  // Discard the file if we cannot infer its asset type from its name.
                                                  AssetType type = ClientFile.InferAssetType(x, requireFullType: false);
                                                  return type.IsValid() ? new AssetFile(x, type) : null;
                                              })
                                              .WhereNotNull()
                                              .ToList();

            if (assetFiles.Count > 0)
            {
                await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
                {
                    DataContext = new ReaderViewModel(_sourceAssetFiles, assetFiles),
                    AutoClose = true
                });
            }
        }
    }
}

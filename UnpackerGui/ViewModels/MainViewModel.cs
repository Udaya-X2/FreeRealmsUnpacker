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

public class MainViewModel : SavedSettingsViewModel
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
    public ReactiveCommand<Unit, Unit> OpenAssetFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenAssetFileCommand { get; }
    public ReactiveCommand<Unit, Unit> AddPackFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddManifestFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddDataFilesCommand { get; }
    public ReactiveCommand<IEnumerable<string>, Unit> AddFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractCheckedFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractSelectedAssetsCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSelectedAssetCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleValidationCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckAllFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> UncheckAllFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveCheckedFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ConvertSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSelectedAssetCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSelectedAssetsCommand { get; }

    private readonly SourceList<AssetFileViewModel> _sourceAssetFiles;
    private readonly ReadOnlyObservableCollection<AssetFileViewModel> _assetFiles;
    private readonly ReadOnlyObservableCollection<AssetFileViewModel> _checkedAssetFiles;
    private readonly PreferencesViewModel _preferences;
    private readonly AboutViewModel _about;

    private int _numAssets;
    private AssetFileViewModel? _selectedAssetFile;
    private AssetInfo? _selectedAsset;
    private IDisposable? _validationHandler;
    private bool _isValidatingAssets;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
    {
        // Initialize each command.
        ExitCommand = ReactiveCommand.Create(App.ShutDown);
        ShowPreferencesCommand = ReactiveCommand.CreateFromTask(ShowPreferences);
        ShowAboutCommand = ReactiveCommand.CreateFromTask(ShowAbout);
        OpenAssetFolderCommand = ReactiveCommand.CreateFromTask(OpenAssetFolder);
        OpenAssetFileCommand = ReactiveCommand.CreateFromTask(OpenAssetFile);
        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        AddManifestFilesCommand = ReactiveCommand.CreateFromTask(AddManifestFiles);
        AddDataFilesCommand = ReactiveCommand.CreateFromTask(AddDataFiles);
        AddFilesCommand = ReactiveCommand.CreateFromTask<IEnumerable<string>>(AddFiles);
        ExtractCheckedFilesCommand = ReactiveCommand.CreateFromTask(ExtractCheckedFiles);
        ExtractSelectedFileCommand = ReactiveCommand.CreateFromTask(ExtractSelectedFile);
        ExtractSelectedAssetsCommand = ReactiveCommand.CreateFromTask(ExtractSelectedAssets);
        SaveSelectedAssetCommand = ReactiveCommand.CreateFromTask(SaveSelectedAsset);
        ToggleValidationCommand = ReactiveCommand.CreateFromTask(ToggleValidation);
        CheckAllFilesCommand = ReactiveCommand.Create(CheckAllFiles);
        UncheckAllFilesCommand = ReactiveCommand.Create(UncheckAllFiles);
        RemoveCheckedFilesCommand = ReactiveCommand.Create(RemoveCheckedFiles);
        RemoveSelectedFileCommand = ReactiveCommand.Create(RemoveSelectedFile);
        ConvertSelectedFileCommand = ReactiveCommand.Create(ConvertSelectedFile);
        OpenSelectedAssetCommand = ReactiveCommand.Create(OpenSelectedAsset);
        ClearSelectedAssetsCommand = ReactiveCommand.Create(ClearSelectedAssets);

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
        SelectedAssets = [];
        CheckedAssets = CheckedAssetFiles.Flatten<ReadOnlyObservableCollection<AssetFileViewModel>, AssetInfo>();
        ValidatedAssets = CheckedAssets.Filter(ValidationOptions);
        Assets = ValidatedAssets.Filter(SearchOptions);

        // Toggle asset validation when requested.
        this.WhenAnyValue(x => x.IsValidatingAssets)
            .Subscribe(_ => ToggleValidationCommand.Invoke());

        // Need to clear selected assets to avoid the UI freezing when a large
        // number of assets are selected while more assets are added/removed.
        Assets.ObserveCollectionChanges()
              .Subscribe(_ => ClearSelectedAssets());

        // Initialize other view models.
        _about = new AboutViewModel();
        _preferences = new PreferencesViewModel(this);
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
    /// Gets the default location to input files/folders asynchronously.
    /// </summary>
    private Task<IStorageFolder?> InputFolder
        => App.GetService<IFilesService>().TryGetFolderFromPathAsync(InputDirectory);

    /// <summary>
    /// Gets the default location to output files/folders asynchronously.
    /// </summary>
    private Task<IStorageFolder?> OutputFolder
        => App.GetService<IFilesService>().TryGetFolderFromPathAsync(OutputDirectory);

    /// <summary>
    /// Opens the Preferences window.
    /// </summary>
    private async Task ShowPreferences() => await App.GetService<IDialogService>().ShowDialog(new PreferencesWindow
    {
        DataContext = _preferences
    });

    /// <summary>
    /// Opens the About Free Realms Unpacker window.
    /// </summary>
    private async Task ShowAbout() => await App.GetService<IDialogService>().ShowDialog(new AboutWindow
    {
        DataContext = _about
    });

    /// <summary>
    /// Opens a folder dialog that allows the user to add asset files in the folder to the source asset files.
    /// </summary>
    private async Task OpenAssetFolder()
    {
        if (await App.GetService<IFilesService>().OpenFolderAsync(new FolderPickerOpenOptions
        {
            SuggestedStartLocation = await InputFolder
        }) is not IStorageFolder folder) return;

        InputDirectory = folder.Path.LocalPath;
        List<AssetFile> assetFiles = [.. ClientDirectory.EnumerateAssetFiles(InputDirectory,
                                                                             AssetFilter,
                                                                             requireFullType: !AddUnknownAssets)
                                                        .ExceptBy(AssetFiles.Select(x => x.FullName), x => x.FullName)];

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
    private async Task OpenAssetFile()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = FileTypeFilters.AssetFiles,
            SuggestedStartLocation = await InputFolder
        });

        if (files.Count == 0) return;

        InputDirectory = Path.GetDirectoryName(files[0].Path.LocalPath) ?? "";
        List<AssetFile> assetFiles = [.. files.Select(x => x.Path.LocalPath)
                                              .Except(AssetFiles.Select(x => x.FullName))
                                              .Select(x => new AssetFile(x))];

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
            FileTypeFilter = fileTypeFilter,
            SuggestedStartLocation = await InputFolder
        });

        if (files.Count == 0) return;

        InputDirectory = Path.GetDirectoryName(files[0].Path.LocalPath) ?? "";
        List<AssetFile> assetFiles = [.. files.Select(x => x.Path.LocalPath)
                                              .Except(AssetFiles.Select(x => x.FullName))
                                              .Select(x => new AssetFile(x, assetType))];

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
    /// Opens a file dialog that allows the user to add asset .dat files to the selected asset file.
    /// </summary>
    private async Task AddDataFiles()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = FileTypeFilters.AssetDatFiles,
            SuggestedStartLocation = await InputFolder
        });

        if (files.Count == 0) return;

        InputDirectory = Path.GetDirectoryName(files[0].Path.LocalPath) ?? "";
        SelectedAssetFile?.DataFiles?.AddRange(files.Select(x => x.Path.LocalPath)
                                                    .Except(SelectedAssetFile.DataFiles.Select(x => x.FullName))
                                                    .Select(x => new DataFileViewModel(x, SelectedAssetFile)));
    }

    /// <summary>
    /// Adds the specified files to the source asset files.
    /// </summary>
    private async Task AddFiles(IEnumerable<string> files)
    {
        List<AssetFile> assetFiles = [.. files.Except(AssetFiles.Select(x => x.FullName))
                                              .Select(x =>
                                              {
                                                  // Discard the file if we cannot infer its asset type from its name.
                                                  AssetType type = ClientFile.InferAssetType(x, requireFullType: false);
                                                  return type.IsValid() ? new AssetFile(x, type) : null;
                                              })
                                              .WhereNotNull()];

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
    /// Opens a folder dialog that allows the user to extract the checked asset files to a directory.
    /// </summary>
    private async Task ExtractCheckedFiles() => await ExtractFiles(CheckedAssetFiles);

    /// <summary>
    /// Opens a folder dialog that allows the user to extract the selected asset file to a directory.
    /// </summary>
    private async Task ExtractSelectedFile() => await ExtractFiles([SelectedAssetFile!]);

    /// <summary>
    /// Opens a folder dialog that allows the user to extract the specified asset files to a directory.
    /// </summary>
    private async Task ExtractFiles(IEnumerable<AssetFileViewModel> assetFiles)
    {
        if (!assetFiles.Any()) return;
        if (await App.GetService<IFilesService>().OpenFolderAsync(new FolderPickerOpenOptions
        {
            SuggestedStartLocation = await OutputFolder
        }) is not IStorageFolder folder) return;

        OutputDirectory = folder.Path.LocalPath;
        await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
        {
            DataContext = new ExtractionViewModel(OutputDirectory, assetFiles, ConflictOptions)
        });
    }

    /// <summary>
    /// Opens a folder dialog that allows the user to extract the selected assets to a directory.
    /// </summary>
    private async Task ExtractSelectedAssets()
    {
        if (SelectedAssets.Count == 0) return;
        if (await App.GetService<IFilesService>().OpenFolderAsync(new FolderPickerOpenOptions
        {
            SuggestedStartLocation = await OutputFolder
        }) is not IStorageFolder folder) return;

        OutputDirectory = folder.Path.LocalPath;
        await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
        {
            DataContext = new ExtractionViewModel(OutputDirectory,
                                                  SelectedAssets.Cast<AssetInfo>(),
                                                  SelectedAssets.Count,
                                                  ConflictOptions)
        });
    }

    /// <summary>
    /// Opens a save file dialog that allows the user to save the selected asset to a file.
    /// </summary>
    private async Task SaveSelectedAsset()
    {
        if (await App.GetService<IFilesService>().SaveFileAsync(new FilePickerSaveOptions
        {
            SuggestedStartLocation = await OutputFolder,
            SuggestedFileName = Path.GetFileName(SelectedAsset!.Name),
            ShowOverwritePrompt = true
        }) is not IStorageFile file) return;

        // Extracting without conflict options here since the explorer will display an overwrite prompt.
        OutputDirectory = Path.GetDirectoryName(file.Path.LocalPath) ?? "";
        using AssetReader reader = SelectedAsset.AssetFile.OpenRead();
        reader.ExtractTo(SelectedAsset with { Name = file.Name }, OutputDirectory);
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
    /// Checks all asset files.
    /// </summary>
    private void CheckAllFiles()
    {
        using (Assets.SuspendNotifications())
        {
            AssetFiles.ForEach(x => x.IsChecked = true);
        }
    }

    /// <summary>
    /// Unchecks all asset files.
    /// </summary>
    private void UncheckAllFiles()
    {
        using (Assets.SuspendNotifications())
        {
            AssetFiles.ForEach(x => x.IsChecked = false);
        }
    }

    /// <summary>
    /// Removes all checked asset files.
    /// </summary>
    private void RemoveCheckedFiles()
    {
        using (Assets.SuspendNotifications())
        {
            _sourceAssetFiles.RemoveMany(CheckedAssetFiles);
        }
    }

    /// <summary>
    /// Removes the selected asset file.
    /// </summary>
    private void RemoveSelectedFile() => _sourceAssetFiles!.Remove(SelectedAssetFile);

    /// <summary>
    /// Converts the selected asset file from a .pack.temp file to a .pack file.
    /// </summary>
    private void ConvertSelectedFile()
    {
        if (ClientFile.TryConvertPackTempFile(SelectedAssetFile!.FullName, out AssetFile? assetFile))
        {
            if (AssetFiles.FirstOrDefault(x => x.FullName == assetFile.FullName) is AssetFileViewModel convertedFile)
            {
                _sourceAssetFiles.Remove(SelectedAssetFile);
            }
            else
            {
                convertedFile = new AssetFileViewModel(assetFile) { IsChecked = SelectedAssetFile.IsChecked };
                _sourceAssetFiles.Replace(SelectedAssetFile, convertedFile);
            }

            SelectedAssetFile = convertedFile;
        }
        else
        {
            throw new Exception($"Unable to convert '{SelectedAssetFile.FullName}' to a .pack file.");
        }
    }

    /// <summary>
    /// Extracts the selected asset to a temporary location and opens it.
    /// </summary>
    private void OpenSelectedAsset()
    {
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory("fru-");
        using AssetReader reader = SelectedAsset!.AssetFile.OpenRead();
        FileInfo file = reader.ExtractTo(SelectedAsset, tempDir.FullName);
        App.GetService<IFilesService>().DeleteOnExit(file.FullName);
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = file.FullName
        });
    }

    /// <summary>
    /// Deselects all selected assets.
    /// </summary>
    private void ClearSelectedAssets()
    {
        SelectedAsset = null;
        SelectedAssets.Clear();
    }
}

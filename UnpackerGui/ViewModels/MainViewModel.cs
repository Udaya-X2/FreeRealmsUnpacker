using AssetIO;
using Avalonia;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using FluentIcons.Common;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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

    /// <summary>
    /// Gets the application's settings.
    /// </summary>
    public SettingsViewModel Settings { get; }

    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowPreferencesCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; }
    public ReactiveCommand<Unit, Unit> AddAssetFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> AddAssetFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddPackFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddManifestFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddDataFilesCommand { get; }
    public ReactiveCommand<IEnumerable<string>, Unit> AddFilesCommand { get; }
    public ReactiveCommand<string, Unit> AddRecentFileCommand { get; }
    public ReactiveCommand<Unit, Unit> AddRecentFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> EmptyRecentFilesCommand { get; }
    public ReactiveCommand<string, Unit> AddRecentFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> EmptyRecentFoldersCommand { get; }
    public ReactiveCommand<Unit, Unit> AddAssetsFromFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AddAssetsFromFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CreatePackFileCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateManifestFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractCheckedFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ExtractSelectedAssetsCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveSelectedAssetCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleValidationCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckAllFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> UncheckAllFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveCheckedFilesCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> CopySelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> RenameSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ReloadSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> ConvertSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSelectedAssetCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearSelectedAssetsCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedAssetsCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectFileCommand { get; }

    private readonly SourceList<AssetFileViewModel> _sourceAssetFiles;
    private readonly ReadOnlyObservableCollection<AssetFileViewModel> _assetFiles;
    private readonly ReadOnlyObservableCollection<AssetFileViewModel> _checkedAssetFiles;
    private readonly PreferencesViewModel _preferences;
    private readonly AboutViewModel _about;

    private int _numAssets;
    private AssetFileViewModel? _selectedAssetFile;
    private AssetInfo? _selectedAsset;
    private IDisposable? _validationHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
    {
        // Initialize each command.
        ExitCommand = ReactiveCommand.Create(App.ShutDown);
        ShowPreferencesCommand = ReactiveCommand.CreateFromTask(ShowPreferences);
        ShowAboutCommand = ReactiveCommand.CreateFromTask(ShowAbout);
        AddAssetFolderCommand = ReactiveCommand.CreateFromTask(AddAssetFolder);
        AddAssetFilesCommand = ReactiveCommand.CreateFromTask(AddAssetFiles);
        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        AddManifestFilesCommand = ReactiveCommand.CreateFromTask(AddManifestFiles);
        AddDataFilesCommand = ReactiveCommand.CreateFromTask(AddDataFiles);
        AddFilesCommand = ReactiveCommand.CreateFromTask<IEnumerable<string>>(AddFiles);
        AddRecentFileCommand = ReactiveCommand.CreateFromTask<string>(AddRecentFile);
        AddRecentFilesCommand = ReactiveCommand.CreateFromTask(AddRecentFiles);
        EmptyRecentFilesCommand = ReactiveCommand.Create(EmptyRecentFiles);
        AddRecentFolderCommand = ReactiveCommand.CreateFromTask<string>(AddRecentFolder);
        EmptyRecentFoldersCommand = ReactiveCommand.Create(EmptyRecentFolders);
        AddAssetsFromFilesCommand = ReactiveCommand.CreateFromTask(AddAssetsFromFiles);
        AddAssetsFromFolderCommand = ReactiveCommand.CreateFromTask(AddAssetsFromFolder);
        CreatePackFileCommand = ReactiveCommand.CreateFromTask(CreatePackFile);
        CreateManifestFileCommand = ReactiveCommand.CreateFromTask(CreateManifestFile);
        ExtractCheckedFilesCommand = ReactiveCommand.CreateFromTask(ExtractCheckedFiles);
        ExtractSelectedFileCommand = ReactiveCommand.CreateFromTask(ExtractSelectedFile);
        ExtractSelectedAssetsCommand = ReactiveCommand.CreateFromTask(ExtractSelectedAssets);
        SaveSelectedAssetCommand = ReactiveCommand.CreateFromTask(SaveSelectedAsset);
        ToggleValidationCommand = ReactiveCommand.CreateFromTask(ToggleValidation);
        CheckAllFilesCommand = ReactiveCommand.Create(CheckAllFiles);
        UncheckAllFilesCommand = ReactiveCommand.Create(UncheckAllFiles);
        RemoveCheckedFilesCommand = ReactiveCommand.Create(RemoveCheckedFiles);
        RemoveSelectedFileCommand = ReactiveCommand.Create(RemoveSelectedFile);
        CopySelectedFileCommand = ReactiveCommand.CreateFromTask(CopySelectedFile);
        RenameSelectedFileCommand = ReactiveCommand.CreateFromTask(RenameSelectedFile);
        DeleteSelectedFileCommand = ReactiveCommand.CreateFromTask(DeleteSelectedFile);
        ClearSelectedFileCommand = ReactiveCommand.CreateFromTask(ClearSelectedFile);
        ReloadSelectedFileCommand = ReactiveCommand.Create(ReloadSelectedFile);
        ConvertSelectedFileCommand = ReactiveCommand.Create(ConvertSelectedFile);
        OpenSelectedAssetCommand = ReactiveCommand.Create(OpenSelectedAsset);
        ClearSelectedAssetsCommand = ReactiveCommand.Create(ClearSelectedAssets);
        DeleteSelectedAssetsCommand = ReactiveCommand.CreateFromTask(DeleteSelectedAssets);
        SelectFileCommand = ReactiveCommand.Create(SelectFile);

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
        Settings = App.GetSettings();
        Settings.WhenAnyValue(x => x.ValidateAssets)
                .Subscribe(_ => ToggleValidationCommand.Invoke());

        // Need to clear selected assets to avoid the UI freezing when a large
        // number of assets are selected while more assets are added/removed.
        Assets.ObserveCollectionChanges()
              .Subscribe(_ => ClearSelectedAssets());

        // Initialize other view models.
        _about = new AboutViewModel();
        _preferences = new PreferencesViewModel();
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
    /// Gets the default location to input files/folders asynchronously.
    /// </summary>
    private Task<IStorageFolder?> InputFolder
        => App.GetService<IFilesService>().TryGetFolderFromPathAsync(Settings.InputDirectory);

    /// <summary>
    /// Gets the default location to output files/folders asynchronously.
    /// </summary>
    private Task<IStorageFolder?> OutputFolder
        => App.GetService<IFilesService>().TryGetFolderFromPathAsync(Settings.OutputDirectory);

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
    private async Task AddAssetFolder()
    {
        if (await App.GetService<IFilesService>().OpenFolderAsync(new FolderPickerOpenOptions
        {
            SuggestedStartLocation = await InputFolder
        }) is not IStorageFolder folder) return;

        Settings.InputDirectory = folder.Path.LocalPath;
        await AddFolder(Settings.InputDirectory);
    }

    /// <summary>
    /// Adds the asset files in the specified folder to the source asset files.
    /// </summary>
    private async Task AddFolder(string folder)
    {
        List<AssetFile> assetFiles = [.. ClientDirectory.EnumerateAssetFiles(folder,
                                                                             Settings.AssetFilter,
                                                                             Settings.SearchOption,
                                                                             !Settings.AddUnknownAssets)
                                                        .ExceptBy(AssetFiles.Select(x => x.FullName), x => x.FullName)];

        if (assetFiles.Count == 0) return;

        Settings.RecentFolders.Add(folder);
        await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
        {
            DataContext = new ReaderViewModel(_sourceAssetFiles, assetFiles),
            AutoClose = true
        });
    }

    /// <summary>
    /// Opens a file dialog that allows the user to add .pack or manifest.dat files to the source asset files.
    /// </summary>
    private async Task AddAssetFiles() => await AddAssetFiles(FileTypeFilters.AssetFiles, null);

    /// <summary>
    /// Opens a file dialog that allows the user to add .pack files to the source asset files.
    /// </summary>
    private async Task AddPackFiles() => await AddAssetFiles(FileTypeFilters.PackFiles, AssetType.Pack);

    /// <summary>
    /// Opens a file dialog that allows the user to add manifest.dat files to the source asset files.
    /// </summary>
    private async Task AddManifestFiles() => await AddAssetFiles(FileTypeFilters.ManifestFiles, AssetType.Dat);

    /// <summary>
    /// Opens a file dialog that allows the user to add asset files of the specified type to the source asset files.
    /// </summary>
    private async Task AddAssetFiles(FilePickerFileType[] fileTypeFilter, AssetType? assetType)
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = fileTypeFilter,
            SuggestedStartLocation = await InputFolder
        });

        if (files.Count == 0) return;

        Settings.InputDirectory = Path.GetDirectoryName(files[0].Path.LocalPath) ?? "";
        await AddFiles(files.Select(x => x.Path.LocalPath), assetType);
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

        Settings.InputDirectory = Path.GetDirectoryName(files[0].Path.LocalPath) ?? "";
        SelectedAssetFile!.DataFiles!.AddRange(files.Select(x => x.Path.LocalPath)
                                                    .Except(SelectedAssetFile.DataFiles.Select(x => x.FullName))
                                                    .Select(x => new DataFileViewModel(x)));
    }

    /// <summary>
    /// Adds the specified files to the source asset files.
    /// </summary>
    private async Task AddFiles(IEnumerable<string> files) => await AddFiles(files, strict: false);

    /// <summary>
    /// Adds the specified files to the source asset files, using the given options.
    /// </summary>
    private async Task AddFiles(IEnumerable<string> files,
                                AssetType? assetType = null,
                                bool requireFullType = false,
                                bool strict = true,
                                bool selectFile = false,
                                bool reloadFile = false)
    {
        List<AssetFile> newAssetFiles = [];
        List<AssetFileViewModel> existingAssetFiles = [];
        Dictionary<string, AssetFileViewModel> nameToAssetFile = AssetFiles.ToDictionary(x => x.FullName);

        foreach (string file in files.Distinct())
        {
            if (nameToAssetFile.TryGetValue(file, out AssetFileViewModel? assetFile))
            {
                existingAssetFiles.Add(assetFile);
                continue;
            }

            AssetType type = assetType ?? ClientFile.InferAssetType(file, requireFullType, strict);

            if (type == 0) continue;

            newAssetFiles.Add(new AssetFile(file, type));
        }

        if (newAssetFiles.Count > 0)
        {
            Settings.RecentFiles.AddItems(newAssetFiles.Select(x => x.FullName));
            await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
            {
                DataContext = new ReaderViewModel(_sourceAssetFiles, newAssetFiles),
                AutoClose = true
            });
        }
        else if (reloadFile)
        {
            existingAssetFiles[0] = ReloadFile(existingAssetFiles[0]);
        }
        if (selectFile)
        {
            AssetFileViewModel? assetFile = existingAssetFiles.FirstOrDefault();
            assetFile ??= AssetFiles.FirstOrDefault(x => (AssetFile)x == newAssetFiles[0]);

            if (assetFile != null)
            {
                SelectedAssetFile = assetFile;
            }
        }
    }

    /// <summary>
    /// Adds the specified recent file to the source asset files.
    /// </summary>
    private async Task AddRecentFile(string file) => await AddFiles([file], selectFile: true);

    /// <summary>
    /// Adds all recent files to the source asset files.
    /// </summary>
    private async Task AddRecentFiles() => await AddFiles(Settings.RecentFiles);

    /// <summary>
    /// Clears the list of recent files.
    /// </summary>
    private void EmptyRecentFiles() => Settings.RecentFiles.Clear();

    /// <summary>
    /// Adds the asset files in the specified recent folder to the source asset files.
    /// </summary>
    private async Task AddRecentFolder(string folder) => await AddFolder(folder);

    /// <summary>
    /// Clears the list of recent folders.
    /// </summary>
    private void EmptyRecentFolders() => Settings.RecentFolders.Clear();

    /// <summary>
    /// Adds the specified assets to the selected asset file.
    /// </summary>
    private async Task AddAssets(List<string> files)
    {
        if (files.Count == 0) return;

        await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
        {
            DataContext = new WriterViewModel(SelectedAssetFile!, files),
            AutoClose = true
        });
        ReloadSelectedFile();
    }

    /// <summary>
    /// Opens a file dialog that allows the user to add assets to the selected asset file.
    /// </summary>
    private async Task AddAssetsFromFiles()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            SuggestedStartLocation = await InputFolder
        });

        if (files.Count == 0) return;

        Settings.InputDirectory = Path.GetDirectoryName(files[0].Path.LocalPath) ?? "";
        await AddAssets([.. files.Select(x => x.Path.LocalPath)]);
    }

    /// <summary>
    /// Opens a folder dialog that allows the user to add assets from it to the selected asset file.
    /// </summary>
    private async Task AddAssetsFromFolder()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        if (await filesService.OpenFolderAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = true,
            SuggestedStartLocation = await InputFolder
        }) is not IStorageFolder folder) return;

        Settings.InputDirectory = folder.Path.LocalPath;
        List<string> files = [.. Directory.EnumerateFiles(Settings.InputDirectory, "*", Settings.SearchOption)];
        await AddAssets(files);
    }

    /// <summary>
    /// Opens a save file dialog that allows the user to create an asset .pack file.
    /// </summary>
    private async Task CreatePackFile() => await CreateAssetFile(AssetType.Pack);

    /// <summary>
    /// Opens a save file dialog that allows the user to create a manifest.dat file.
    /// </summary>
    private async Task CreateManifestFile() => await CreateAssetFile(AssetType.Dat);

    /// <summary>
    /// Opens a save file dialog that allows the user to create an asset file of the specified type.
    /// </summary>
    private async Task CreateAssetFile(AssetType assetType)
    {
        bool isPackFile = (assetType & AssetType.Pack) != 0;
        if (await App.GetService<IFilesService>().SaveFileAsync(new FilePickerSaveOptions
        {
            SuggestedStartLocation = await OutputFolder,
            SuggestedFileName = isPackFile ? "Assets_000.pack" : "Assets_manifest.dat",
            FileTypeChoices = isPackFile ? FileTypeFilters.PackFiles : FileTypeFilters.ManifestFiles,
            ShowOverwritePrompt = true,
            Title = "Create"
        }) is not IStorageFile file) return;

        AssetFile assetFile = new(file.Path.LocalPath, assetType);
        Settings.OutputDirectory = assetFile.DirectoryName ?? "";
        assetFile.Create();
        await AddFiles([assetFile.FullName], selectFile: true, reloadFile: true);
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

        Settings.OutputDirectory = folder.Path.LocalPath;
        await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
        {
            DataContext = new ExtractionViewModel(assetFiles),
            AutoClose = true
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

        Settings.OutputDirectory = folder.Path.LocalPath;
        await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
        {
            DataContext = new ExtractionViewModel(SelectedAssets.Cast<AssetInfo>(), SelectedAssets.Count),
            AutoClose = true
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
        Settings.OutputDirectory = Path.GetDirectoryName(file.Path.LocalPath) ?? "";
        using AssetReader reader = SelectedAsset.AssetFile.OpenRead();
        reader.ExtractTo(SelectedAsset with { Name = file.Name }, Settings.OutputDirectory);
    }

    /// <summary>
    /// Toggles whether to validate assets in checked asset files.
    /// </summary>
    private async Task ToggleValidation()
    {
        if (Settings.ValidateAssets)
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
    private void RemoveSelectedFile() => _sourceAssetFiles.Remove(SelectedAssetFile!);

    /// <summary>
    /// Opens a save file dialog that allows the user to copy the selected asset file.
    /// </summary>
    private async Task CopySelectedFile()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        if (await filesService.SaveFileAsync(new FilePickerSaveOptions
        {
            SuggestedStartLocation = await filesService.TryGetFolderFromPathAsync(SelectedAssetFile!.DirectoryName!),
            SuggestedFileName = GetCopyFileName(SelectedAssetFile.Name, SelectedAssetFile.DirectoryName ?? ""),
            ShowOverwritePrompt = true,
            Title = "Copy"
        }) is not IStorageFile file) return;
        if (!SelectedAssetFile.CopyTo(file.Path.LocalPath)) return;

        await AddFiles([file.Path.LocalPath], selectFile: true, reloadFile: true);
    }

    /// <summary>
    /// Returns a unique copy file name based on the given file name and directory name.
    /// </summary>
    private static string GetCopyFileName(string fileName, string dirName)
    {
        string copyFileName = $"Copy of {fileName}";

        for (int i = 2; File.Exists(Path.Combine(dirName, copyFileName)); i++)
        {
            copyFileName = $"Copy ({i}) of {fileName}";
        }

        return copyFileName;
    }

    /// <summary>
    /// Opens a save file dialog that allows the user to rename the selected asset file.
    /// </summary>
    private async Task RenameSelectedFile()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        if (await filesService.SaveFileAsync(new FilePickerSaveOptions
        {
            SuggestedStartLocation = await filesService.TryGetFolderFromPathAsync(SelectedAssetFile!.DirectoryName!),
            SuggestedFileName = SelectedAssetFile.Name,
            ShowOverwritePrompt = true,
            Title = "Rename"
        }) is not IStorageFile file) return;
        if (!SelectedAssetFile.MoveTo(file.Path.LocalPath)) return;

        _sourceAssetFiles.Remove(x => x != SelectedAssetFile && x.FullName == SelectedAssetFile.FullName);
    }

    /// <summary>
    /// Opens a confirm dialog that allows the user to delete the selected asset file.
    /// </summary>
    private async Task DeleteSelectedFile()
    {
        if (!Settings.ConfirmDelete || await App.GetService<IDialogService>().ShowConfirmDialog(new ConfirmViewModel
        {
            Title = "Delete File",
            Message = $"Are you sure you want to permanently delete this file?\n\n{SelectedAssetFile}",
            Icon = Icon.Delete,
            CheckBoxMessage = "Delete asset .dat files",
            CheckBoxCommand = ReactiveCommand.Create(() => Settings.DeleteDataFiles ^= true),
            IsChecked = Settings.DeleteDataFiles,
            ShowCheckBox = SelectedAssetFile!.FileType == AssetType.Dat
        }))
        {
            SelectedAssetFile!.Delete();
            RemoveSelectedFile();
        }
    }

    /// <summary>
    /// Opens a confirm dialog that allows the user to clear all assets from the selected asset file.
    /// </summary>
    private async Task ClearSelectedFile()
    {
        if (!Settings.ConfirmDelete || await App.GetService<IDialogService>().ShowConfirmDialog(new ConfirmViewModel
        {
            Title = "Clear File",
            Message = $"Are you sure you want to clear all assets from this file?\n\n{SelectedAssetFile}"
        }))
        {
            SelectedAssetFile!.Create();
            ReloadSelectedFile();
        }
    }

    /// <summary>
    /// Reloads the selected asset file.
    /// </summary>
    private void ReloadSelectedFile() => ReloadFile(SelectedAssetFile!);

    /// <summary>
    /// Reloads the specified asset file.
    /// </summary>
    private AssetFileViewModel ReloadFile(AssetFileViewModel assetFile)
    {
        bool refreshSelected = assetFile == SelectedAssetFile;
        AssetFileViewModel reloadedFile = assetFile.Reload();
        _sourceAssetFiles.Replace(assetFile, reloadedFile);

        if (refreshSelected)
        {
            SelectedAssetFile = reloadedFile;
        }

        return reloadedFile;
    }

    /// <summary>
    /// Converts the selected asset file from a .pack.temp file to a .pack file.
    /// </summary>
    private void ConvertSelectedFile()
    {
        AssetFile assetFile = ClientFile.ConvertPackTempFile(SelectedAssetFile!);

        if (AssetFiles.FirstOrDefault(x => x.FullName == assetFile.FullName) is AssetFileViewModel convertedFile)
        {
            _sourceAssetFiles.Remove(SelectedAssetFile!);
        }
        else
        {
            convertedFile = new AssetFileViewModel(assetFile) { IsChecked = SelectedAssetFile!.IsChecked };
            _sourceAssetFiles.Replace(SelectedAssetFile, convertedFile);
        }

        SelectedAssetFile = convertedFile;
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

    /// <summary>
    /// Deletes all selected assets from their corresponding asset files.
    /// </summary>
    private async Task DeleteSelectedAssets()
    {
        if (!Settings.ConfirmDelete || await App.GetService<IDialogService>().ShowConfirmDialog(new ConfirmViewModel
        {
            Title = SelectedAssets.Count == 1 ? "Delete Asset" : "Delete Multiple Assets",
            Message = SelectedAssets.Count == 1
            ? $"Are you sure you want to permanently delete this asset?\n\n{SelectedAsset}"
            : $"Are you sure you want to permanently delete these {SelectedAssets.Count} assets?",
            Icon = Icon.Delete
        }))
        {
            using (Assets.SuspendNotifications())
            {
                DeleteViewModel deleteViewModel = new(SelectedAssets.Cast<AssetInfo>());
                await App.GetService<IDialogService>().ShowDialog(new ProgressWindow
                {
                    DataContext = deleteViewModel,
                    AutoClose = true
                });
                AssetFiles.Where(x => deleteViewModel.ModifiedFiles.Contains(x))
                          .ToList()
                          .ForEach(x => ReloadFile(x));
            }
        }
    }

    /// <summary>
    /// Selects the asset file containing the selected asset.
    /// </summary>
    private void SelectFile() => SelectedAssetFile = AssetFiles.First(x => x.Contains(SelectedAsset!));
}

using AssetIO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnpackerGui.Collections;
using UnpackerGui.Models;
using UnpackerGui.Services;

namespace UnpackerGui.ViewModels;

public class AssetFileViewModel : ViewModelBase, IList<AssetInfo>
{
    /// <summary>
    /// Gets the data files corresponding to the asset file.
    /// </summary>
    /// <remarks>This is only used by asset files with the <see cref="AssetType.Dat"/> flag set.</remarks>
    public ReactiveList<DataFileViewModel>? DataFiles { get; }


    /// <summary>
    /// Gets the selected data files.
    /// </summary>
    /// <remarks>This is only used by asset files with the <see cref="AssetType.Dat"/> flag set.</remarks>
    public ReactiveList<DataFileViewModel>? SelectedDataFiles { get; }

    /// <summary>
    /// Gets the total size of all assets in the file.
    /// </summary>
    public long Size { get; }

    public ReactiveCommand<Unit, bool>? ShowDataFilesCommand { get; }
    public ReactiveCommand<Unit, Unit>? RemoveDataFilesCommand { get; }
    public ReactiveCommand<Unit, Unit>? DeleteDataFilesCommand { get; }
    public ReactiveCommand<Unit, Unit>? RenameDataFileCommand { get; }

    private readonly AssetFile _assetFile;
    private readonly List<AssetInfo> _assets;

    private bool _isChecked;
    private bool _showDataFiles;
    private bool _isValidated;
    private DataFileViewModel? _selectedDataFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetFileViewModel"/> class.
    /// </summary>
    public AssetFileViewModel(AssetFile assetFile, CancellationToken token = default)
    {
        _assets = [];
        _assetFile = assetFile;

        if (Design.IsDesignMode) return;

        foreach (Asset asset in _assetFile)
        {
            token.ThrowIfCancellationRequested();
            _assets.Add(new AssetInfo(asset, _assetFile));
            Size += asset.Size;
        }

        if (FileType == AssetType.Dat)
        {
            DataFiles = [.. assetFile.DataFiles.Select(x => new DataFileViewModel(x))];
            assetFile.DataFiles = DataFiles.Select(x => x.FullName);
            SelectedDataFiles = [];
            ShowDataFilesCommand = ReactiveCommand.Create(() => ShowDataFiles ^= true);
            RemoveDataFilesCommand = ReactiveCommand.Create(RemoveDataFiles);
            DeleteDataFilesCommand = ReactiveCommand.Create(DeleteDataFiles);
            RenameDataFileCommand = ReactiveCommand.CreateFromTask(RenameDataFile);
        }
    }

    /// <summary>
    /// Gets or sets wehther the asset file is checked.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    /// <summary>
    /// Gets or sets whether to show the data files.
    /// </summary>
    public bool ShowDataFiles
    {
        get => _showDataFiles;
        set => this.RaiseAndSetIfChanged(ref _showDataFiles, value);
    }

    /// <summary>
    /// Gets or sets whether the asset file has been validated.
    /// </summary>
    public bool IsValidated
    {
        get => _isValidated;
        set => this.RaiseAndSetIfChanged(ref _isValidated, value);
    }

    /// <summary>
    /// Gets or sets the selected data file.
    /// </summary>
    public DataFileViewModel? SelectedDataFile
    {
        get => _selectedDataFile;
        set => this.RaiseAndSetIfChanged(ref _selectedDataFile, value);
    }

    /// <inheritdoc cref="AssetFile.Name"/>
    public string Name => _assetFile.Name;

    /// <inheritdoc cref="AssetFile.FullName"/>
    public string FullName => _assetFile.FullName;

    /// <inheritdoc cref="AssetFile.DirectoryName"/>
    public string? DirectoryName => _assetFile.DirectoryName;

    /// <inheritdoc cref="AssetFile.Type"/>
    public AssetType Type => _assetFile.Type;

    /// <inheritdoc cref="AssetFile.FileType"/>
    public AssetType FileType => _assetFile.FileType;

    /// <inheritdoc cref="AssetFile.DirectoryType"/>
    public AssetType DirectoryType => _assetFile.DirectoryType;

    /// <inheritdoc/>
    public int Count => _assets.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public AssetInfo this[int index]
    {
        get => _assets[index];
        set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
    }

    /// <inheritdoc cref="AssetFile.OpenRead"/>
    public AssetReader OpenRead() => _assetFile.OpenRead();

    /// <inheritdoc cref="AssetFile.OpenWrite"/>
    public AssetWriter OpenWrite() => _assetFile.OpenWrite();

    /// <inheritdoc cref="AssetFile.OpenAppend"/>
    public AssetWriter OpenAppend() => _assetFile.OpenAppend();

    /// <summary>
    /// Moves the asset file to a new location, providing the option to specify a new file name.
    /// </summary>
    public void MoveTo(string destFileName)
    {
        if (FullName != destFileName)
        {
            _assetFile.Info.MoveTo(destFileName, overwrite: true);
            this.RaisePropertyChanged(nameof(Name));
            this.RaisePropertyChanged(nameof(FullName));
            this.RaisePropertyChanged(nameof(DirectoryName));
        }
    }

    /// <summary>
    /// Permanently deletes the asset file.
    /// </summary>
    public void Delete() => _assetFile.Info.Delete();

    /// <summary>
    /// Reloads the asset file, returning a new <see cref="AssetFileViewModel"/> with the updated data.
    /// </summary>
    public AssetFileViewModel Reload(CancellationToken token = default)
    {
        _assetFile.Refresh();
        return new(_assetFile, token)
        {
            _isChecked = _isChecked,
            _showDataFiles = _showDataFiles,
            _isValidated = false,
            _selectedDataFile = null
        };
    }

    /// <summary>
    /// Removes the selected data files from the asset file.
    /// </summary>
    private void RemoveDataFiles()
    {
        DataFileViewModel[] dataFiles = [.. SelectedDataFiles!];
        SelectedDataFiles!.Clear();
        DataFiles!.RemoveMany(dataFiles);
    }

    /// <summary>
    /// Deletes the selected data files.
    /// </summary>
    private void DeleteDataFiles()
    {
        SelectedDataFiles!.ForEach(x => x.Delete());
        RemoveDataFiles();
    }

    /// <summary>
    /// Opens a save file dialog that allows the user to rename the selected data file.
    /// </summary>
    private async Task RenameDataFile()
    {
        IFilesService filesService = App.GetService<IFilesService>();
        if (await filesService.SaveFileAsync(new FilePickerSaveOptions
        {
            SuggestedStartLocation = await filesService.TryGetFolderFromPathAsync(SelectedDataFile!.DirectoryName!),
            SuggestedFileName = SelectedDataFile.Name,
            ShowOverwritePrompt = true,
            Title = "Rename"
        }) is not IStorageFile file) return;
        SelectedDataFile.MoveTo(file.Path.LocalPath);
    }

    /// <inheritdoc/>
    public int IndexOf(AssetInfo item) => _assets.IndexOf(item);

    /// <inheritdoc/>
    public bool Contains(AssetInfo item) => item.AssetFile == _assetFile;

    /// <inheritdoc/>
    public void CopyTo(AssetInfo[] array, int arrayIndex) => _assets.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<AssetInfo> GetEnumerator() => _assets.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _assets.GetEnumerator();

    /// <inheritdoc/>
    void IList<AssetInfo>.Insert(int index, AssetInfo item)
        => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

    /// <inheritdoc/>
    void IList<AssetInfo>.RemoveAt(int index)
        => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

    /// <inheritdoc/>
    void ICollection<AssetInfo>.Add(AssetInfo item)
        => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

    /// <inheritdoc/>
    void ICollection<AssetInfo>.Clear()
        => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);

    /// <inheritdoc/>
    bool ICollection<AssetInfo>.Remove(AssetInfo item)
        => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
}

﻿using AssetIO;
using Avalonia.Platform.Storage;
using FluentIcons.Common;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnpackerGui.Collections;
using UnpackerGui.Converters;
using UnpackerGui.Extensions;
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
            DeleteDataFilesCommand = ReactiveCommand.CreateFromTask(DeleteDataFiles);
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

    /// <inheritdoc cref="AssetFile.Type"/>
    public AssetType Type => _assetFile.Type;

    /// <inheritdoc cref="AssetFile.FileType"/>
    public AssetType FileType => _assetFile.FileType;

    /// <inheritdoc cref="AssetFile.DirectoryType"/>
    public AssetType DirectoryType => _assetFile.DirectoryType;

    /// <inheritdoc cref="AssetFile.Attributes"/>
    public FileAttributes Attributes => _assetFile.Attributes;

    /// <inheritdoc cref="AssetFile.CreationTime"/>
    public DateTime CreationTime => _assetFile.CreationTime;

    /// <inheritdoc cref="AssetFile.CreationTimeUtc"/>
    public DateTime CreationTimeUtc => _assetFile.CreationTimeUtc;

    /// <inheritdoc cref="AssetFile.Directory"/>
    public DirectoryInfo? Directory => _assetFile.Directory;

    /// <inheritdoc cref="AssetFile.DirectoryName"/>
    public string? DirectoryName => _assetFile.DirectoryName;

    /// <inheritdoc cref="AssetFile.Exists"/>
    public bool Exists => _assetFile.Exists;

    /// <inheritdoc cref="AssetFile.Extension"/>
    public string Extension => _assetFile.Extension;

    /// <inheritdoc cref="AssetFile.FullName"/>
    public string FullName => _assetFile.FullName;

    /// <inheritdoc cref="AssetFile.IsReadOnly"/>
    public bool IsReadOnly => _assetFile.IsReadOnly;

    /// <inheritdoc cref="AssetFile.LastAccessTime"/>
    public DateTime LastAccessTime => _assetFile.LastAccessTime;

    /// <inheritdoc cref="AssetFile.LastAccessTimeUtc"/>
    public DateTime LastAccessTimeUtc => _assetFile.LastAccessTimeUtc;

    /// <inheritdoc cref="AssetFile.LastWriteTime"/>
    public DateTime LastWriteTime => _assetFile.LastWriteTime;

    /// <inheritdoc cref="AssetFile.LastWriteTimeUtc"/>
    public DateTime LastWriteTimeUtc => _assetFile.LastWriteTimeUtc;

    /// <inheritdoc cref="AssetFile.Length"/>
    public long Length => _assetFile.Length;

    /// <inheritdoc cref="AssetFile.LinkTarget"/>
    public string? LinkTarget => _assetFile.LinkTarget;

    /// <inheritdoc cref="AssetFile.Name"/>
    public string Name => _assetFile.Name;

    /// <inheritdoc cref="AssetFile.UnixFileMode"/>
    public UnixFileMode UnixFileMode => _assetFile.UnixFileMode;

    /// <inheritdoc cref="AssetFile.UnixFileMode"/>
    public int Count => _assets.Count;

    /// <inheritdoc/>
    bool ICollection<AssetInfo>.IsReadOnly => true;

    /// <inheritdoc/>
    public AssetInfo this[int index]
    {
        get => _assets[index];
        set => throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
    }

    /// <summary>
    /// Returns the backing asset file.
    /// </summary>
    public static implicit operator AssetFile(AssetFileViewModel assetFile) => assetFile._assetFile;

    /// <inheritdoc cref="AssetFile.OpenRead"/>
    public AssetReader OpenRead() => _assetFile.OpenRead();

    /// <inheritdoc cref="AssetFile.OpenWrite"/>
    public AssetWriter OpenWrite() => _assetFile.OpenWrite();

    /// <inheritdoc cref="AssetFile.OpenAppend"/>
    public AssetWriter OpenAppend() => _assetFile.OpenAppend();

    /// <summary>
    /// Copies the existing asset file to a new file.
    /// </summary>
    /// <returns><see langword="true"/> if the asset file was copied; otherwise, <see langword="false"/>.</returns>
    public bool CopyTo(string destFileName)
    {
        if (FullName == destFileName) return false;

        _assetFile.Info.CopyTo(destFileName, overwrite: true);

        if (FileType != AssetType.Dat) return true;

        var dataFiles = Enumerable.Zip(DataFiles!, ClientDirectory.EnumerateDataFilesInfinite(destFileName));

        foreach ((DataFileViewModel srcDataFile, string destDataFile) in dataFiles)
        {
            srcDataFile.CopyTo(destDataFile);
        }

        return true;
    }

    /// <summary>
    /// Moves the asset file to a new location, providing the option to specify a new file name.
    /// </summary>
    /// <returns><see langword="true"/> if the asset file was moved; otherwise, <see langword="false"/>.</returns>
    public bool MoveTo(string destFileName)
    {
        if (FullName == destFileName) return false;

        _assetFile.Info.MoveTo(destFileName, overwrite: true);
        this.RaisePropertyChanged(nameof(Name));
        this.RaisePropertyChanged(nameof(FullName));
        this.RaisePropertyChanged(nameof(DirectoryName));
        return true;
    }

    /// <summary>
    /// Permanently deletes the asset file.
    /// </summary>
    public void Delete()
    {
        _assetFile.Info.Delete();

        if (FileType == AssetType.Dat && Settings.DeleteDataFiles)
        {
            DataFiles!.ForEach(x => x.Delete());
        }
    }

    /// <summary>
    /// Creates or overwrites the asset file.
    /// </summary>
    public void Create() => _assetFile.Create();

    /// <summary>
    /// Refreshes the asset file's attributes, returning a reference to this instance after the operation is completed.
    /// </summary>
    public AssetFileViewModel Refresh()
    {
        _assetFile.Refresh();
        return this;
    }

    /// <summary>
    /// Reloads the asset file, returning a new <see cref="AssetFileViewModel"/> with the updated data.
    /// </summary>
    public AssetFileViewModel Reload(CancellationToken token = default)
    {
        AssetFile assetFile = new(_assetFile.FullName, _assetFile.Type);
        assetFile.Refresh();
        return new(assetFile, token)
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
    /// Opens a confirm dialog that allows the user to delete the selected data files.
    /// </summary>
    private async Task DeleteDataFiles()
    {
        if (!Settings.ConfirmDelete || await App.GetService<IDialogService>().ShowConfirmDialog(new ConfirmViewModel
        {
            Title = SelectedDataFiles!.Count == 1 ? "Delete File" : "Delete Multiple Files",
            Message = SelectedDataFiles.Count == 1
            ? $"Are you sure you want to permanently delete this file?\n\n{SelectedDataFile}"
            : $"Are you sure you want to permanently delete these {SelectedDataFiles.Count} files?",
            Icon = Icon.Delete
        }))
        {
            SelectedDataFiles!.ForEach(x => x.Delete());
            RemoveDataFiles();
        }
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
        DataFiles!.Remove(x => x != SelectedDataFile && x.FullName == SelectedDataFile.FullName);
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

    /// <summary>
    /// Returns a string representation of the asset file's properties.
    /// </summary>
    /// <returns>A string representation of the asset file's properties.</returns>
    public override string ToString()
        => $"{Name}\nType: {Type}\nCount: {Count}\n"
           + $"Size: {FileSizeConverter.GetFileSize(Size)}\nLocation: {DirectoryName}";
}

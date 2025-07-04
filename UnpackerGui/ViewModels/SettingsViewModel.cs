﻿using AssetIO;
using ReactiveUI;
using System.IO;

namespace UnpackerGui.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _showName = true;
    private bool _showOffset = true;
    private bool? _showSize = null;
    private bool _showCrc32 = true;
    private bool _validateAssets = false;
    private FileConflictOptions _conflictOptions = FileConflictOptions.Overwrite;
    private AssetType _assetFilter = AssetType.All;
    private bool _addUnknownAssets = false;
    private SearchOption _searchOption = SearchOption.AllDirectories;
    private bool _confirmDelete = true;
    private bool _deleteDataFiles = true;
    private string _inputDirectory = "";
    private string _outputDirectory = "";

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
    public bool? ShowSize
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
    /// Gets or sets whether to validate assets in checked files.
    /// </summary>
    public bool ValidateAssets
    {
        get => _validateAssets;
        set => this.RaiseAndSetIfChanged(ref _validateAssets, value);
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
    /// Gets or sets which assets to search for in folders.
    /// </summary>
    public AssetType AssetFilter
    {
        get => _assetFilter;
        set => this.RaiseAndSetIfChanged(ref _assetFilter, value);
    }

    /// <summary>
    /// Gets or sets whether to search for unknown assets in folders.
    /// </summary>
    public bool AddUnknownAssets
    {
        get => _addUnknownAssets;
        set => this.RaiseAndSetIfChanged(ref _addUnknownAssets, value);
    }

    /// <summary>
    /// Gets or sets whether to search for assets in folders recursively.
    /// </summary>
    public SearchOption SearchOption
    {
        get => _searchOption;
        set => this.RaiseAndSetIfChanged(ref _searchOption, value);
    }

    /// <summary>
    /// Gets or sets whether to ask the user to confirm file deletion.
    /// </summary>
    public bool ConfirmDelete
    {
        get => _confirmDelete;
        set => this.RaiseAndSetIfChanged(ref _confirmDelete, value);
    }

    /// <summary>
    /// Gets or sets whether to delete a manifest.dat file's accompanying asset .dat files.
    /// </summary>
    public bool DeleteDataFiles
    {
        get => _deleteDataFiles;
        set => this.RaiseAndSetIfChanged(ref _deleteDataFiles, value);
    }

    /// <summary>
    /// Gets or sets the default location to input files/folders.
    /// </summary>
    public string InputDirectory
    {
        get => _inputDirectory;
        set => this.RaiseAndSetIfChanged(ref _inputDirectory, value);
    }

    /// <summary>
    /// Gets or sets the default location to output files/folders.
    /// </summary>
    public string OutputDirectory
    {
        get => _outputDirectory;
        set => this.RaiseAndSetIfChanged(ref _outputDirectory, value);
    }
}

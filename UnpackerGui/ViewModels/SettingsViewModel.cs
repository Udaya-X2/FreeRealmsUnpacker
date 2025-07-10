using AssetIO;
using ReactiveUI;
using System;
using System.IO;
using System.Text.Json.Serialization;
using UnpackerGui.Collections;

namespace UnpackerGui.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _showName = true;
    private bool _showOffset = true;
    private bool? _showSize = null;
    private bool _showCrc32 = true;
    private bool _showType = false;
    private bool _validateAssets = false;
    private FileConflictOptions _conflictOptions = FileConflictOptions.Overwrite;
    private AssetType _assetFilter = AssetType.All;
    private bool _addUnknownAssets = false;
    private SearchOption _searchOption = SearchOption.AllDirectories;
    private bool _confirmDelete = true;
    private bool _deleteDataFiles = true;
    private bool _copyColumnHeaders = false;
    private string _clipboardSeparator = "\t";
    private string _clipboardLineSeparator = Environment.NewLine;
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
    /// Gets or sets whether to show the asset's file type.
    /// </summary>
    public bool ShowType
    {
        get => _showType;
        set => this.RaiseAndSetIfChanged(ref _showType, value);
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
    [JsonConverter(typeof(JsonStringEnumConverter<FileConflictOptions>))]
    public FileConflictOptions ConflictOptions
    {
        get => _conflictOptions;
        set => this.RaiseAndSetIfChanged(ref _conflictOptions, value);
    }

    /// <summary>
    /// Gets or sets which assets to search for in folders.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<AssetType>))]
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
    [JsonConverter(typeof(JsonStringEnumConverter<SearchOption>))]
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
    /// Gets or sets whether to include the column headers when copying assets to the clipboard.
    /// </summary>
    public bool CopyColumnHeaders
    {
        get => _copyColumnHeaders;
        set => this.RaiseAndSetIfChanged(ref _copyColumnHeaders, value);
    }

    /// <summary>
    /// Gets or sets the separator used when copying assets to the clipboard.
    /// </summary>
    public string ClipboardSeparator
    {
        get => _clipboardSeparator;
        set => this.RaiseAndSetIfChanged(ref _clipboardSeparator, value);
    }

    /// <summary>
    /// Gets or sets the line separator used when copying assets to the clipboard.
    /// </summary>
    public string ClipboardLineSeparator
    {
        get => _clipboardLineSeparator;
        set => this.RaiseAndSetIfChanged(ref _clipboardLineSeparator, value);
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

    /// <summary>
    /// Gets or sets the recently used files.
    /// </summary>
    [JsonConverter(typeof(JsonRecentItemCollectionConverter<string>))]
    public RecentItemCollection<string> RecentFiles { get; init; } = [];

    /// <summary>
    /// Gets or sets the recently used folders.
    /// </summary>
    [JsonConverter(typeof(JsonRecentItemCollectionConverter<string>))]
    public RecentItemCollection<string> RecentFolders { get; init; } = [];
}

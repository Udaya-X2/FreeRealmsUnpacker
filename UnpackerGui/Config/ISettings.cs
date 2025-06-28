using AssetIO;
using System.ComponentModel;
using System.IO;

namespace UnpackerGui.Config;

public interface ISettings : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets whether to show the asset's name.
    /// </summary>
    [DefaultValue(true)]
    bool ShowName { get; set; }

    /// <summary>
    /// Gets or sets whether to show the asset's offset.
    /// </summary>
    [DefaultValue(true)]
    bool ShowOffset { get; set; }

    /// <summary>
    /// Gets or sets whether to show the asset's size.
    /// </summary>
    [DefaultValue(null)]
    bool? ShowSize { get; set; }

    /// <summary>
    /// Gets or sets whether to show the asset's CRC-32.
    /// </summary>
    [DefaultValue(true)]
    bool ShowCrc32 { get; set; }

    /// <summary>
    /// Gets or sets whether to validate assets in checked files.
    /// </summary>
    [DefaultValue(false)]
    bool ValidateAssets { get; set; }

    /// <summary>
    /// Gets or sets how to handle assets with conflicting names.
    /// </summary>
    [DefaultValue(FileConflictOptions.Overwrite)]
    FileConflictOptions ConflictOptions { get; set; }

    /// <summary>
    /// Gets or sets which assets to search for in folders.
    /// </summary>
    [DefaultValue(AssetType.All)]
    AssetType AssetFilter { get; set; }

    /// <summary>
    /// Gets or sets whether to search for unknown assets in folders.
    /// </summary>
    [DefaultValue(false)]
    bool AddUnknownAssets { get; set; }

    /// <summary>
    /// Gets or sets whether to search for assets in folders recursively.
    /// </summary>
    [DefaultValue(SearchOption.AllDirectories)]
    SearchOption SearchOption { get; set; }

    /// <summary>
    /// Gets or sets whether to ask the user to confirm file deletion.
    /// </summary>
    [DefaultValue(true)]
    bool ConfirmDelete { get; set; }

    /// <summary>
    /// Gets or sets the default location to input files/folders.
    /// </summary>
    [DefaultValue("")]
    string InputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the default location to output files/folders.
    /// </summary>
    [DefaultValue("")]
    string OutputDirectory { get; set; }
}

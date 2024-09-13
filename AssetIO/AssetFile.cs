using System.Collections;
using System.ComponentModel;

namespace AssetIO;

/// <summary>
/// Provides properties and instance methods for the reading and extraction of Free Realms asset files.
/// </summary>
public class AssetFile : IEnumerable<Asset>
{
    /// <summary>
    /// Gets the file info of the asset file.
    /// </summary>
    public FileInfo Info { get; }

    /// <summary>
    /// Gets the asset type of the asset file.
    /// </summary>
    public AssetType Type { get; }

    /// <summary>
    /// Gets or sets the asset.dat files corresponding to the asset file.
    /// </summary>
    /// <remarks>This is only used by asset files with the <see cref="AssetType.Dat"/> flag set.</remarks>
    public IEnumerable<string> DataFiles { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified asset .pack file or manifest.dat file.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public AssetFile(string path)
        : this(path, ClientFile.InferAssetType(path, requireFullType: false, strict: true))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified
    /// asset .pack file or manifest.dat file, with the specified asset type.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public AssetFile(string path, AssetType assetType)
        : this(path, assetType, null, findDataFiles: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified
    /// asset .pack file or manifest.dat file, with the specified data files.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public AssetFile(string path, IEnumerable<string> dataFiles)
        : this(path, ClientFile.InferAssetType(path, requireFullType: false, strict: true), dataFiles)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified asset
    /// .pack file or manifest.dat file, with the specified asset type and data files.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public AssetFile(string path, AssetType assetType, IEnumerable<string> dataFiles)
        : this(path, assetType, dataFiles, findDataFiles: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified asset
    /// .pack file or manifest.dat file, with the specified asset type and data files.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    private AssetFile(string path, AssetType assetType, IEnumerable<string>? dataFiles, bool findDataFiles)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (!assetType.IsValid()) throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, assetType));

        Info = new FileInfo(path);
        Type = assetType;
        DataFiles = (findDataFiles, FileType) switch
        {
            (true, AssetType.Dat) => ClientDirectory.EnumerateDataFiles(Info),
            (true, _) => Enumerable.Empty<string>(),
            (false, _) => dataFiles ?? throw new ArgumentNullException(nameof(dataFiles))
        };
    }

    /// <summary>
    /// Gets the file flag of the asset file type.
    /// </summary>
    public AssetType FileType => Type.GetFileType();

    /// <summary>
    /// Gets the directory flag of the asset file type.
    /// </summary>
    public AssetType DirectoryType => Type.GetDirectoryType();

    /// <summary>
    /// Gets the name of the asset file.
    /// </summary>
    public string Name => Info.Name;

    /// <summary>
    /// Gets the full path of the asset file.
    /// </summary>
    public string FullName => Info.FullName;

    /// <summary>
    /// Gets the number of assets in the asset file.
    /// </summary>
    public int Count => FileType switch
    {
        AssetType.Dat => ClientFile.GetManifestAssetCount(FullName),
        AssetType.Pack => ClientFile.GetPackAssetCount(FullName),
        _ => throw new InvalidEnumArgumentException(nameof(Type), (int)Type, Type.GetType())
    };

    /// <summary>
    /// Gets the assets in the asset file.
    /// </summary>
    public Asset[] Assets => FileType switch
    {
        AssetType.Dat => ClientFile.GetManifestAssets(FullName),
        AssetType.Pack => ClientFile.GetPackAssets(FullName),
        _ => throw new InvalidEnumArgumentException(nameof(Type), (int)Type, Type.GetType())
    };

    /// <summary>
    /// Creates an <see cref="AssetReader"/> that reads from the asset file or related data files.
    /// </summary>
    /// <returns>A new <see cref="AssetReader"/> that reads from the asset file or related data files.</returns>
    public AssetReader OpenRead() => FileType switch
    {
        AssetType.Dat => new AssetDatReader(DataFiles),
        AssetType.Pack => new AssetPackReader(FullName),
        _ => throw new InvalidEnumArgumentException(nameof(Type), (int)Type, Type.GetType())
    };

    /// <summary>
    /// Returns an enumerable collection of the assets in the asset file.
    /// </summary>
    /// <returns>An enumerable collection of the assets in the asset file.</returns>
    public IEnumerable<Asset> EnumerateAssets() => FileType switch
    {
        AssetType.Dat => ClientFile.EnumerateManifestAssets(FullName),
        AssetType.Pack => ClientFile.EnumeratePackAssets(FullName),
        _ => throw new InvalidEnumArgumentException(nameof(Type), (int)Type, Type.GetType())
    };

    /// <summary>
    /// Returns an enumerator that iterates through the assets in the asset file.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the assets in the asset file.</returns>
    public IEnumerator<Asset> GetEnumerator() => EnumerateAssets().GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

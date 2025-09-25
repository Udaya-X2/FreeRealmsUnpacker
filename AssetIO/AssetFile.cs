using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace AssetIO;

/// <summary>
/// Provides properties and instance methods for the reading, writing,
/// extraction, and validation of Free Realms asset files.
/// </summary>
public class AssetFile : IEnumerable<Asset>
{
    /// <summary>
    /// Gets the file info of the asset file.
    /// </summary>
    public virtual FileInfo Info { get; }

    /// <summary>
    /// Gets the asset type of the asset file.
    /// </summary>
    public virtual AssetType Type { get; }

    /// <summary>
    /// Gets or sets the asset .dat files corresponding to the asset file.
    /// </summary>
    /// <remarks>This is only used by asset files with the <see cref="AssetType.Dat"/> flag set.</remarks>
    public virtual IEnumerable<string> DataFiles { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified asset .pack file or manifest.dat file.
    /// </summary>
    /// <param name="path">The path to the asset .pack file or manifest.dat file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public AssetFile(string path)
        : this(path, ClientFile.InferAssetType(path, requireFullType: false, strict: true))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified
    /// asset .pack file or manifest.dat file, with the specified asset type.
    /// </summary>
    /// <param name="path">The path to the asset .pack file or manifest.dat file.</param>
    /// <param name="assetType">Specifies the type of the asset file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public AssetFile(string path, AssetType assetType)
        : this(path, assetType, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified
    /// asset .pack file or manifest.dat file, with the specified data files.
    /// </summary>
    /// <param name="path">The path to the asset .pack file or manifest.dat file.</param>
    /// <param name="dataFiles">The asset .dat files to read from, if necessary.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public AssetFile(string path, [AllowNull] IEnumerable<string> dataFiles)
        : this(path, ClientFile.InferAssetType(path, requireFullType: false, strict: true), dataFiles)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AssetFile"/> from the specified asset
    /// .pack file or manifest.dat file, with the specified asset type and data files.
    /// </summary>
    /// <param name="path">The path to the asset .pack file or manifest.dat file.</param>
    /// <param name="assetType">Specifies the type of the asset file.</param>
    /// <param name="dataFiles">The asset .dat files to read from, if necessary.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public AssetFile(string path, AssetType assetType, [AllowNull] IEnumerable<string> dataFiles)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!assetType.IsValid()) ThrowHelper.ThrowArgument_InvalidAssetType(assetType);

        Info = new FileInfo(path);
        Type = assetType;
        DataFiles = dataFiles ?? (FileType == AssetType.Dat ? ClientDirectory.EnumerateDataFiles(Info) : []);
    }

    /// <summary>
    /// Gets the file flag of the asset file type.
    /// </summary>
    public virtual AssetType FileType => Type.GetFileType();

    /// <summary>
    /// Gets the directory flag of the asset file type.
    /// </summary>
    public virtual AssetType DirectoryType => Type.GetDirectoryType();

    /// <inheritdoc cref="FileSystemInfo.Attributes"/>
    public virtual FileAttributes Attributes => Info.Attributes;

    /// <inheritdoc cref="FileSystemInfo.CreationTime"/>
    public virtual DateTime CreationTime => Info.CreationTime;

    /// <inheritdoc cref="FileSystemInfo.CreationTimeUtc"/>
    public virtual DateTime CreationTimeUtc => Info.CreationTimeUtc;

    /// <inheritdoc cref="FileInfo.Directory"/>
    public virtual DirectoryInfo? Directory => Info.Directory;

    /// <inheritdoc cref="FileInfo.DirectoryName"/>
    public virtual string? DirectoryName => Info.DirectoryName;

    /// <inheritdoc cref="FileInfo.Exists"/>
    public virtual bool Exists => Info.Exists;

    /// <inheritdoc cref="FileSystemInfo.Extension"/>
    public virtual string Extension => Info.Extension;

    /// <inheritdoc cref="FileSystemInfo.FullName"/>
    public virtual string FullName => Info.FullName;

    /// <inheritdoc cref="FileInfo.IsReadOnly"/>
    public virtual bool IsReadOnly => Info.IsReadOnly;

    /// <inheritdoc cref="FileSystemInfo.LastAccessTime"/>
    public virtual DateTime LastAccessTime => Info.LastAccessTime;

    /// <inheritdoc cref="FileSystemInfo.LastAccessTimeUtc"/>
    public virtual DateTime LastAccessTimeUtc => Info.LastAccessTimeUtc;

    /// <inheritdoc cref="FileSystemInfo.LastWriteTime"/>
    public virtual DateTime LastWriteTime => Info.LastWriteTime;

    /// <inheritdoc cref="FileSystemInfo.LastWriteTimeUtc"/>
    public virtual DateTime LastWriteTimeUtc => Info.LastWriteTimeUtc;

    /// <inheritdoc cref="FileInfo.Length"/>
    public virtual long Length => Info.Length;

    /// <inheritdoc cref="FileSystemInfo.LinkTarget"/>
    public virtual string? LinkTarget => Info.LinkTarget;

    /// <inheritdoc cref="FileInfo.Name"/>
    public virtual string Name => Info.Name;

    /// <inheritdoc cref="FileSystemInfo.UnixFileMode"/>
    public virtual UnixFileMode UnixFileMode => Info.UnixFileMode;

    /// <summary>
    /// Gets the number of assets in the asset file.
    /// </summary>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OverflowException"/>
    public virtual int Count => FileType switch
    {
        AssetType.Pack => ClientFile.GetPackAssetCount(FullName),
        AssetType.Dat => ClientFile.GetManifestAssetCount(FullName),
        AssetType.Pack | AssetType.Temp => ClientFile.GetPackTempAssetCount(FullName),
        _ => ThrowHelper.ThrowArgument_InvalidAssetType<int>(Type)
    };

    /// <summary>
    /// Gets the assets in the asset file.
    /// </summary>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OutOfMemoryException"/>
    public virtual Asset[] Assets => FileType switch
    {
        AssetType.Pack => ClientFile.GetPackAssets(FullName),
        AssetType.Dat => ClientFile.GetManifestAssets(FullName),
        AssetType.Pack | AssetType.Temp => ClientFile.GetPackTempAssets(FullName),
        _ => ThrowHelper.ThrowArgument_InvalidAssetType<Asset[]>(Type)
    };

    /// <summary>
    /// Creates an <see cref="AssetReader"/> that reads from the asset file or related data files.
    /// </summary>
    /// <param name="bufferSize">A non-negative integer value indicating the buffer size.</param>
    /// <returns>A new <see cref="AssetReader"/> that reads from the asset file or related data files.</returns>
    /// <exception cref="IOException"/>
    public virtual AssetReader OpenRead(int bufferSize = Constants.BufferSize) => (FileType & ~AssetType.Temp) switch
    {
        AssetType.Pack => new AssetPackReader(FullName, bufferSize),
        AssetType.Dat => new AssetDatReader(DataFiles, bufferSize),
        _ => ThrowHelper.ThrowArgument_InvalidAssetType<AssetReader>(Type)
    };

    /// <summary>
    /// Creates an <see cref="AssetWriter"/> that writes to the asset file and related data files.
    /// </summary>
    /// <param name="bufferSize">A non-negative integer value indicating the buffer size.</param>
    /// <returns>A new <see cref="AssetReader"/> that writes to the asset file and related data files.</returns>
    /// <exception cref="IOException"/>
    /// <exception cref="NotSupportedException"/>
    public virtual AssetWriter OpenWrite(int bufferSize = Constants.BufferSize) => FileType switch
    {
        AssetType.Pack => new AssetPackWriter(FullName, append: false, bufferSize),
        AssetType.Dat => new AssetDatWriter(FullName, DataFiles, append: false, bufferSize),
        AssetType.Pack | AssetType.Temp => ThrowHelper.ThrowNotSupported_PackTempWrite<AssetWriter>(),
        _ => ThrowHelper.ThrowArgument_InvalidAssetType<AssetWriter>(Type)
    };

    /// <summary>
    /// Creates an <see cref="AssetWriter"/> that appends to the asset file and related data files.
    /// </summary>
    /// <param name="bufferSize">A non-negative integer value indicating the buffer size.</param>
    /// <returns>A new <see cref="AssetReader"/> that appends to the asset file and related data files.</returns>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="NotSupportedException"/>
    public virtual AssetWriter OpenAppend(int bufferSize = Constants.BufferSize) => FileType switch
    {
        AssetType.Pack => new AssetPackWriter(FullName, append: true, bufferSize),
        AssetType.Dat => new AssetDatWriter(FullName, DataFiles, append: true, bufferSize),
        AssetType.Pack | AssetType.Temp => ThrowHelper.ThrowNotSupported_PackTempWrite<AssetWriter>(),
        _ => ThrowHelper.ThrowArgument_InvalidAssetType<AssetWriter>(Type)
    };

    /// <summary>
    /// Creates or overwrites the asset file.
    /// </summary>
    /// <exception cref="IOException"/>
    /// <exception cref="NotSupportedException"/>
    public virtual void Create() => OpenWrite(0).Dispose();

    /// <summary>
    /// Extracts the assets from the asset file to the given directory.
    /// </summary>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="options">Specifies how to handle file conflicts in the destination path.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public virtual void ExtractAssets(string destDir, FileConflictOptions options = FileConflictOptions.Overwrite)
    {
        ArgumentNullException.ThrowIfNull(destDir);

        switch (FileType)
        {
            case AssetType.Pack:
                ClientFile.ExtractPackAssets(FullName, destDir, options);
                break;
            case AssetType.Dat:
                ClientFile.ExtractManifestAssets(FullName, DataFiles, destDir, options);
                break;
            case AssetType.Pack | AssetType.Temp:
                ClientFile.ExtractPackTempAssets(FullName, destDir, options);
                break;
            default:
                ThrowHelper.ThrowArgument_InvalidAssetType(Type);
                break;
        }
    }

    /// <inheritdoc cref="RemoveAssets(ISet{Asset})"/>
    public virtual void RemoveAssets(IEnumerable<Asset> assets) => RemoveAssets(assets.ToHashSet());

    /// <summary>
    /// Removes the specified assets from the asset file.
    /// </summary>
    /// <param name="assets">The assets to remove from the asset file.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public virtual void RemoveAssets(ISet<Asset> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

        switch (FileType)
        {
            case AssetType.Pack:
                ClientFile.RemovePackAssets(FullName, assets);
                break;
            case AssetType.Dat:
                ClientFile.RemoveManifestAssets(FullName, DataFiles, assets);
                break;
            case AssetType.Pack | AssetType.Temp:
                ThrowHelper.ThrowNotSupported_PackTempWrite();
                break;
            default:
                ThrowHelper.ThrowArgument_InvalidAssetType(Type);
                break;
        }
    }

    /// <summary>
    /// Throws an exception if the asset file is invalid or contains assets with CRC-32 mismatches.
    /// </summary>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public virtual void ValidateAssets()
    {
        switch (FileType)
        {
            case AssetType.Pack:
                ClientFile.ValidatePackAssets(FullName);
                break;
            case AssetType.Dat:
                ClientFile.ValidateManifestAssets(FullName, DataFiles);
                break;
            case AssetType.Pack | AssetType.Temp:
                ClientFile.ValidatePackTempAssets(FullName);
                break;
            default:
                ThrowHelper.ThrowArgument_InvalidAssetType(Type);
                break;
        }
    }

    /// <summary>
    /// Refreshes the asset file's attributes.
    /// </summary>
    /// <exception cref="IOException"/>
    public virtual void Refresh() => Info.Refresh();

    /// <summary>
    /// Returns an enumerable collection of the assets in the asset file.
    /// </summary>
    /// <returns>An enumerable collection of the assets in the asset file.</returns>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public virtual IEnumerable<Asset> EnumerateAssets() => FileType switch
    {
        AssetType.Pack => ClientFile.EnumeratePackAssets(FullName),
        AssetType.Dat => ClientFile.EnumerateManifestAssets(FullName),
        AssetType.Pack | AssetType.Temp => ClientFile.EnumeratePackTempAssets(FullName),
        _ => ThrowHelper.ThrowArgument_InvalidAssetType<IEnumerable<Asset>>(Type)
    };

    /// <summary>
    /// Returns an enumerator that iterates through the assets in the asset file.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the assets in the asset file.</returns>
    public virtual IEnumerator<Asset> GetEnumerator() => EnumerateAssets().GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns the full path of the asset file.
    /// </summary>
    /// <returns>The full path of the asset file.</returns>
    public override string ToString() => Info.FullName;
}

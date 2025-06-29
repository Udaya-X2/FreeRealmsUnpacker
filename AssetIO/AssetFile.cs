﻿using System.Collections;
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
        if (!assetType.IsValid()) throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, assetType));

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

    /// <summary>
    /// Gets the name of the asset file.
    /// </summary>
    public virtual string Name => Info.Name;

    /// <summary>
    /// Gets the full path of the asset file.
    /// </summary>
    public virtual string FullName => Info.FullName;

    /// <summary>
    /// Gets a string representing the directory's full path.
    /// </summary>
    public virtual string? DirectoryName => Info.DirectoryName;

    /// <summary>
    /// Gets the number of assets in the asset file.
    /// </summary>
    public virtual int Count => FileType switch
    {
        AssetType.Pack => ClientFile.GetPackAssetCount(FullName),
        AssetType.Dat => ClientFile.GetManifestAssetCount(FullName),
        AssetType.Pack | AssetType.Temp => ClientFile.GetPackTempAssetCount(FullName),
        _ => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, Type))
    };

    /// <summary>
    /// Gets the assets in the asset file.
    /// </summary>
    public virtual Asset[] Assets => FileType switch
    {
        AssetType.Pack => ClientFile.GetPackAssets(FullName),
        AssetType.Dat => ClientFile.GetManifestAssets(FullName),
        AssetType.Pack | AssetType.Temp => ClientFile.GetPackTempAssets(FullName),
        _ => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, Type))
    };

    /// <summary>
    /// Creates an <see cref="AssetReader"/> that reads from the asset file or related data files.
    /// </summary>
    /// <returns>A new <see cref="AssetReader"/> that reads from the asset file or related data files.</returns>
    public virtual AssetReader OpenRead() => (FileType & ~AssetType.Temp) switch
    {
        AssetType.Pack => new AssetPackReader(FullName),
        AssetType.Dat => new AssetDatReader(DataFiles),
        _ => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, Type))
    };

    /// <summary>
    /// Creates an <see cref="AssetWriter"/> that writes to the asset file and related data files.
    /// </summary>
    /// <returns>A new <see cref="AssetReader"/> that writes to the asset file and related data files.</returns>
    /// <exception cref="NotSupportedException"/>
    public virtual AssetWriter OpenWrite() => FileType switch
    {
        AssetType.Pack => new AssetPackWriter(FullName),
        AssetType.Dat => new AssetDatWriter(FullName, DataFiles),
        AssetType.Pack | AssetType.Temp => throw new NotSupportedException(SR.NotSupported_PackTempWrite),
        _ => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, Type))
    };

    /// <summary>
    /// Creates an <see cref="AssetWriter"/> that appends to the asset file and related data files.
    /// </summary>
    /// <returns>A new <see cref="AssetReader"/> that appends to the asset file and related data files.</returns>
    public virtual AssetWriter OpenAppend() => FileType switch
    {
        AssetType.Pack => new AssetPackWriter(FullName, append: true),
        AssetType.Dat => new AssetDatWriter(FullName, DataFiles, append: true),
        AssetType.Pack | AssetType.Temp => throw new NotSupportedException(SR.NotSupported_PackTempWrite),
        _ => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, Type))
    };

    /// <summary>
    /// Creates or overwrites the asset file.
    /// </summary>
    public virtual void Create() => OpenWrite().Dispose();

    /// <summary>
    /// Throws an exception if the asset file is invalid or contains assets with CRC-32 mismatches.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
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
                throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, Type));
        }
    }

    /// <summary>
    /// Refreshes the asset file's attributes and related data files.
    /// </summary>
    /// <exception cref="IOException"/>
    public virtual void Refresh()
    {
        Info.Refresh();
        DataFiles = FileType == AssetType.Dat ? ClientDirectory.EnumerateDataFiles(Info) : [];
    }

    /// <summary>
    /// Returns an enumerable collection of the assets in the asset file.
    /// </summary>
    /// <returns>An enumerable collection of the assets in the asset file.</returns>
    public virtual IEnumerable<Asset> EnumerateAssets() => FileType switch
    {
        AssetType.Pack => ClientFile.EnumeratePackAssets(FullName),
        AssetType.Dat => ClientFile.EnumerateManifestAssets(FullName),
        AssetType.Pack | AssetType.Temp => ClientFile.EnumeratePackTempAssets(FullName),
        _ => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, Type))
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

using AssetIO.EndianBinaryIO;
using Force.Crc32;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetIO;

/// <summary>
/// Provides static methods for obtaining asset information from a Free Realms asset file.
/// </summary>
public static partial class ClientFile
{
    private const int ManifestChunkSize = 148; // The size of an asset chunk in a manifest .dat file.
    private const int MaxAssetNameLength = 128; // The maximum asset name length allowed in a manifest .dat file.
    private const int MaxAssetDatSize = 209715200; // The maximum possible size of an asset .dat file.
    private const int DefaultAssetsPerChunk = 128; // Default # of assets to write per chunk in a .pack file.
    private const int SizeOfPackAssetFields = 16; // sizeof(Name.Length) + sizeof(Offset) + sizeof(Size) + sizeof(Crc32)
    private const int SizeOfPackAssetChunkHeader = 8; // sizeof(NextOffset) + sizeof(NumAssets)
    private const int BufferSize = 81920;
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
    private const string PackFileSuffix = ".pack";
    private const string ManifestFileSuffix = "_manifest.dat";
    private const string PackTempFileSuffix = ".pack.temp";
    private const string DatFileSuffix = ".dat";

    [GeneratedRegex(@"^Assets(W?_\d{3}\.pack(\.temp)?|_manifest\.dat)$", Options, "en-US")]
    private static partial Regex GameAssetRegex();
    [GeneratedRegex(@"^assetpack000(W?_\d{3}\.pack(\.temp)?|_manifest\.dat)$", Options, "en-US")]
    private static partial Regex TcgAssetRegex();
    [GeneratedRegex(@"^AssetsTcg(W?_\d{3}\.pack(\.temp)?|_manifest\.dat)$", Options, "en-US")]
    private static partial Regex ResourceAssetRegex();
    [GeneratedRegex(@"^assets_ps3w?_\d{3}\.pack(\.temp)?$", Options, "en-US")]
    private static partial Regex PS3AssetRegex();
    [GeneratedRegex(@"^Assets_\d{3}\.dat$", Options, "en-US")]
    private static partial Regex GameDataRegex();
    [GeneratedRegex(@"^assetpack000_\d{3}\.dat$", Options, "en-US")]
    private static partial Regex TcgDataRegex();
    [GeneratedRegex(@"^AssetsTcg_\d{3}\.dat$", Options, "en-US")]
    private static partial Regex ResourceDataRegex();

    /// <summary>
    /// Returns an enumerable collection of the assets in the specified .pack file.
    /// </summary>
    /// <returns>An enumerable collection of the assets in the specified .pack file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static IEnumerable<Asset> EnumeratePackAssets(string packFile)
    {
        ArgumentNullException.ThrowIfNull(packFile);

        // Read the .pack file in big-endian format.
        using FileStream stream = File.OpenRead(packFile);
        using EndianBinaryReader reader = new(stream, Endian.Big);
        uint nextOffset = 0;
        uint numAssets = 0;
        Asset asset;

        // .pack files store assets via two types of data:
        // 
        // * Asset information chunks, which specify what
        //   assets are in the file and where they are.
        // 
        // * Asset content chunks, which store the bytes of each
        //   asset in sequential, contiguous regions of memory.
        // 
        // Asset information chunks begin with a preamble in the following format:
        // 
        // Positions    Sample Value    Description
        // 1-4          2170048         Offset of the next asset info chunk.
        // 5-8          197             Number of assets in the current chunk.
        // 
        // Following the preamble, each asset in the chunk has the following format:
        // 
        // Positions    Sample Value    Description
        // 1-4          13              Length of the asset's name, in bytes.
        // 5-X          ComicBook.lst   Name of the asset.
        // X-X+4        1889108         Offset of the asset in the asset .pack file.
        // X+4-X+8      338             Size of the asset, in bytes.
        // X+8-X+12     1085288468      CRC-32 checksum of the asset.
        // 
        // Scan each asset info chunk of the .pack file for information regarding each asset.
        do
        {
            try
            {
                stream.Position = nextOffset;
                nextOffset = reader.ReadUInt32();
                numAssets = reader.ReadUInt32();
            }
            catch (EndOfStreamException ex)
            {
                throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, stream.Name), ex);
            }

            for (uint i = 0; i < numAssets; i++)
            {
                try
                {
                    int length = ValidateRange(reader.ReadInt32(), minValue: 1, maxValue: MaxAssetNameLength);
                    string name = reader.ReadString(length);
                    uint offset = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();
                    uint crc32 = reader.ReadUInt32();
                    asset = new Asset(name, offset, size, crc32);
                }
                catch (ArgumentOutOfRangeException ex) when (ex.Data["BytesRead"] is int bytesRead)
                {
                    throw new IOException(string.Format(SR.IO_BadAsset, stream.Position - bytesRead, stream.Name), ex);
                }
                catch (EndOfStreamException ex)
                {
                    throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, stream.Name), ex);
                }

                yield return asset;
            }
        }
        while (nextOffset != 0);
    }

    /// <summary>
    /// Returns the assets in the specified .pack file.
    /// </summary>
    /// <returns>An array consisting of the assets in the specified .pack file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static Asset[] GetPackAssets(string packFile) => packFile == null
        ? throw new ArgumentNullException(nameof(packFile))
        : [.. EnumeratePackAssets(packFile)];

    /// <summary>
    /// Returns the number of assets in the specified .pack file.
    /// </summary>
    /// <returns>The number of assets in the specified .pack file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="OverflowException"/>
    public static int GetPackAssetCount(string packFile)
    {
        ArgumentNullException.ThrowIfNull(packFile);

        // Read the .pack file in big-endian format.
        using FileStream stream = new(packFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8);
        using EndianBinaryReader reader = new(stream, Endian.Big);
        uint nextOffset = 0;
        uint numAssets = 0;

        // Read the number of assets in each asset info chunk.
        try
        {
            checked
            {
                do
                {
                    stream.Position = nextOffset;
                    nextOffset = reader.ReadUInt32();
                    numAssets += reader.ReadUInt32();
                }
                while (nextOffset != 0);

                return (int)numAssets;
            }
        }
        catch (EndOfStreamException ex)
        {
            throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, stream.Name), ex);
        }
        catch (OverflowException ex)
        {
            throw new OverflowException(string.Format(SR.Overflow_TooManyAssets, stream.Name), ex);
        }
    }

    /// <inheritdoc cref="AppendPackAssets(string, IEnumerable{FileInfo})"/>
    public static void AppendPackAssets(string packFile, IEnumerable<string> assets)
        => AppendPackAssets(packFile, assets.Select(x => new FileInfo(x)));

    /// <summary>
    /// Opens a .pack file, appends the specified assets to the file, and then closes
    /// the file. If the target file does not exist, this method creates the file.
    /// </summary>
    public static void AppendPackAssets(string packFile, IEnumerable<FileInfo> assets)
    {
        ArgumentNullException.ThrowIfNull(packFile);
        ArgumentNullException.ThrowIfNull(assets);

        // Open the .pack file in big-endian format.
        using FileStream stream = new(packFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        using EndianBinaryWriter writer = new(stream, Endian.Big);
        using IEnumerator<FileInfo> enumerator = assets.GetEnumerator();
        bool hasNext = enumerator.MoveNext();
        long fileSize = stream.Length;

        // Exit early if there are no assets to add.
        if (!hasNext)
        {
            // If this is an empty .pack file, write an empty asset chunk at the start of the file.
            if (fileSize == 0)
            {
                stream.Write(stackalloc byte[SizeOfPackAssetChunkHeader]);
            }

            return;
        }

        // Keep track of chunk assets via FileStream to prevent concurrent modification (and optimize Length reads).
        List<FileStream> chunkAssets = new(DefaultAssetsPerChunk);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        FileInfo file = enumerator.Current;
        uint chunkOffset = 0;
        uint assetOffset = 0;
        long prevOffset = -1;

        try
        {
            checked
            {
                // If this is a non-empty .pack file, skip to the last asset chunk.
                if (fileSize != 0)
                {
                    using EndianBinaryReader reader = new(stream, Endian.Big, Encoding.UTF8, leaveOpen: true);

                    while ((chunkOffset = reader.ReadUInt32()) != 0)
                    {
                        stream.Position = chunkOffset;
                    }

                    // If the last asset chunk is empty and at the end of the .pack file, overwrite it.
                    if (stream.Position + sizeof(uint) == fileSize)
                    {
                        stream.Seek(-sizeof(uint), SeekOrigin.Current);
                    }
                    // Otherwise, create a new asset chunk at the end of the .pack file.
                    else
                    {
                        assetOffset = chunkOffset = (uint)fileSize;
                        stream.Seek(-sizeof(uint), SeekOrigin.Current);
                        prevOffset = stream.Position;
                        writer.Write(chunkOffset);
                        stream.Position = chunkOffset;
                    }
                }

                while (hasNext)
                {
                    // Add assets to the current asset chunk.
                    file = enumerator.Current;
                    FileStream asset = file.OpenRead();
                    int fileNameLength = ValidateRange(file.Name.Length, minValue: 1, maxValue: MaxAssetNameLength);
                    chunkOffset += (uint)asset.Length;
                    assetOffset += (uint)fileNameLength + SizeOfPackAssetFields;
                    chunkAssets.Add(asset);
                    hasNext = enumerator.SafeMoveNext();

                    // If this is the last asset, write the asset chunk and set
                    // the next asset chunk to the start of the .pack file.
                    if (!hasNext)
                    {
                        WritePackAssetChunk(writer, chunkAssets, chunkOffset: 0, assetOffset, buffer);
                    }
                    // When the asset chunk is at max capacity, write the assets to the .pack file.
                    else if (chunkAssets.Count == DefaultAssetsPerChunk)
                    {
                        chunkOffset += assetOffset + SizeOfPackAssetChunkHeader;
                        prevOffset = stream.Position;
                        WritePackAssetChunk(writer, chunkAssets, chunkOffset, assetOffset, buffer);
                        assetOffset = chunkOffset;
                        chunkOffset = 0;
                    }
                }
            }
        }
        catch (EndOfStreamException ex)
        {
            throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, stream.Name), ex);
        }
        catch (OverflowException ex)
        {
            throw new OverflowException(string.Format(SR.Overflow_MaxPackCapacity, file, stream.Name), ex);
        }
        catch (ArgumentOutOfRangeException ex) when (ex.Data.Contains("BytesRead"))
        {
            throw new PathTooLongException(string.Format(SR.PathTooLong_CantAddAsset, file, stream.Name), ex);
        }
        finally
        {
            chunkAssets.ForEach(x => x.Dispose());
            ArrayPool<byte>.Shared.Return(buffer);

            // If an error occurred before all assets were written, ensure
            // the previous asset chunk is the last one in the .pack file.
            if (hasNext && prevOffset != -1)
            {
                stream.Position = prevOffset;
                writer.Write(0u); // Next Offset
            }
        }
    }

    /// <inheritdoc cref="WritePackAssets(string, IEnumerable{FileInfo})"/>
    public static void WritePackAssets(string packFile, IEnumerable<string> assets)
        => WritePackAssets(packFile, assets.Select(x => new FileInfo(x)));

    /// <summary>
    /// Creates a new .pack file, writes the specified assets to the file, and then
    /// closes the file. If the target file already exists, it is overwritten.
    /// </summary>
    public static void WritePackAssets(string packFile, IEnumerable<FileInfo> assets)
    {
        ArgumentNullException.ThrowIfNull(packFile);
        ArgumentNullException.ThrowIfNull(assets);

        File.Delete(packFile);
        AppendPackAssets(packFile, assets);
    }

    /// <summary>
    /// Returns an enumerable collection of the assets in the specified manifest.dat file.
    /// </summary>
    /// <returns>An enumerable collection of the assets in the specified manifest.dat file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static IEnumerable<Asset> EnumerateManifestAssets(string manifestFile)
    {
        ArgumentNullException.ThrowIfNull(manifestFile);

        // Read the manifest.dat file in little-endian format.
        using FileStream stream = File.OpenRead(manifestFile);
        using EndianBinaryReader reader = new(stream, Endian.Little);
        long numAssets = stream.Length / ManifestChunkSize;
        Asset asset;

        if (stream.Length % ManifestChunkSize != 0)
        {
            throw new IOException(string.Format(SR.IO_BadManifest, stream.Name));
        }

        // manifest.dat files are divided into 148-byte chunks of data with the following format:
        // 
        // Positions    Sample Value    Description
        // 1-4          14              Length of the asset's name, in bytes.
        // 5-X          mines_pink.def  Name of the asset.
        // X-X+8        826             Offset of the asset in the asset .dat files(s).
        // X+8-X+12     25              Size of the asset, in bytes.
        // X+12-X+16    3577151519      CRC-32 checksum of the asset.
        // X+16-148     0               Null bytes for the rest of the chunk.
        // 
        // Scan each manifest chunk for information regarding each asset.
        for (int i = 0; i < numAssets; i++)
        {
            try
            {
                int length = ValidateRange(reader.ReadInt32(), minValue: 1, maxValue: MaxAssetNameLength);
                string name = reader.ReadString(length);
                long offset = ValidateRange(reader.ReadInt64(), minValue: 0, maxValue: long.MaxValue);
                uint size = reader.ReadUInt32();
                uint crc32 = reader.ReadUInt32();
                asset = new Asset(name, offset, size, crc32);
                stream.Seek(MaxAssetNameLength - length, SeekOrigin.Current);
            }
            catch (ArgumentOutOfRangeException ex) when (ex.Data["BytesRead"] is int bytesRead)
            {
                throw new IOException(string.Format(SR.IO_BadAsset, stream.Position - bytesRead, stream.Name), ex);
            }

            yield return asset;
        }
    }

    /// <summary>
    /// Returns the assets in the specified manifest.dat file.
    /// </summary>
    /// <returns>An array consisting of the assets in the specified manifest.dat file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static Asset[] GetManifestAssets(string manifestFile)
    {
        ArgumentNullException.ThrowIfNull(manifestFile);

        Asset[] assets = new Asset[GetManifestAssetCount(manifestFile)];
        int i = 0;

        foreach (Asset asset in EnumerateManifestAssets(manifestFile))
        {
            assets[i++] = asset;
        }

        return assets;
    }

    /// <summary>
    /// Returns the number of assets in the specified manifest.dat file.
    /// </summary>
    /// <returns>The number of assets in the specified manifest.dat file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OverflowException"/>
    public static int GetManifestAssetCount(string manifestFile)
    {
        ArgumentNullException.ThrowIfNull(manifestFile);

        FileInfo manifestFileInfo = new(manifestFile);

        try
        {
            return manifestFileInfo.Length % ManifestChunkSize == 0
                ? checked((int)(manifestFileInfo.Length / ManifestChunkSize))
                : throw new IOException(string.Format(SR.IO_BadManifest, manifestFile));
        }
        catch (OverflowException ex)
        {
            throw new OverflowException(string.Format(SR.Overflow_TooManyAssets, manifestFileInfo.Name), ex);
        }
    }

    /// <inheritdoc cref="AppendManifestAssets(string, IEnumerable{FileInfo})"/>
    public static void AppendManifestAssets(string manifestFile, IEnumerable<string> assets)
        => AppendManifestAssets(manifestFile, assets.Select(x => new FileInfo(x)));

    /// <summary>
    /// Opens a manifest .dat file, appends the specified assets to the file, and then
    /// closes the file. If the target file does not exist, this method creates the file.
    /// </summary>
    public static void AppendManifestAssets(string manifestFile, IEnumerable<FileInfo> assets)
    {
        ArgumentNullException.ThrowIfNull(manifestFile);
        ArgumentNullException.ThrowIfNull(assets);

        using AssetDatWriter writer = new(manifestFile, append: true);

        foreach (FileInfo asset in assets)
        {
            writer.Write(asset);
        }
    }

    /// <inheritdoc cref="WriteManifestAssets(string, IEnumerable{FileInfo})"/>
    public static void WriteManifestAssets(string manifestFile, IEnumerable<string> assets)
        => WriteManifestAssets(manifestFile, assets.Select(x => new FileInfo(x)));

    /// <summary>
    /// Creates a new manifest .dat file, writes the specified assets to the file, and
    /// then closes the file. If the target file already exists, it is overwritten.
    /// </summary>
    public static void WriteManifestAssets(string manifestFile, IEnumerable<FileInfo> assets)
    {
        ArgumentNullException.ThrowIfNull(manifestFile);
        ArgumentNullException.ThrowIfNull(assets);

        using AssetDatWriter writer = new(manifestFile);

        foreach (FileInfo asset in assets)
        {
            writer.Write(asset);
        }
    }

    /// <summary>
    /// Returns an enumerable collection of the valid assets in the specified .pack.temp file.
    /// </summary>
    /// <returns>An enumerable collection of the valid assets in the specified .pack.temp file.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<Asset> EnumeratePackTempAssets(string packTempFile)
    {
        ArgumentNullException.ThrowIfNull(packTempFile);

        using IEnumerator<Asset> enumerator = EnumeratePackAssets(packTempFile).GetEnumerator();
        long end = new FileInfo(packTempFile).Length;

        while (true)
        {
            try
            {
                // Stop iteration if there are no more assets in the file.
                if (!enumerator.MoveNext())
                {
                    break;
                }
            }
            // Stop iteration if an error occurs due to cut off data in the file.
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or EndOfStreamException)
            {
                break;
            }

            Asset asset = enumerator.Current;

            // Check whether the current asset extends past the end of the file.
            if (end - asset.Offset - asset.Size < 0) break;

            yield return asset;
        }
    }

    /// <summary>
    /// Gets the valid assets in the specified .pack.temp file.
    /// </summary>
    /// <returns>Gets the valid assets in the specified .pack.temp file.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static Asset[] GetPackTempAssets(string packTempFile)
        => [.. EnumeratePackTempAssets(packTempFile)];

    /// <summary>
    /// Returns the number of the valid assets in the specified .pack.temp file.
    /// </summary>
    /// <returns>An number of the valid assets in the specified .pack.temp file.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static int GetPackTempAssetCount(string packTempFile)
        => EnumeratePackTempAssets(packTempFile).Count();

    /// <summary>
    /// Attempts to scan the specified .pack.temp file for errors and create a fix for the first invalid asset group.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the .pack.temp file contains a fixable error; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public static bool TryGetPackTempFix(string packTempFile, out FixedAssetGroup fix)
    {
        ArgumentNullException.ThrowIfNull(packTempFile);

        // Read the .pack.temp file in big-endian format.
        using FileStream stream = File.OpenRead(packTempFile);
        using EndianBinaryReader reader = new(stream, Endian.Big);
        long end = stream.Length;
        uint prevOffset = 0;
        uint currOffset = 0;
        uint nextOffset = 0;
        uint currAsset = 0;
        uint numAssets = 0;

        // Scan each asset chunk for errors.
        try
        {
            do
            {
                currAsset = 0;
                prevOffset = currOffset;
                stream.Position = currOffset = nextOffset;
                nextOffset = reader.ReadUInt32();
                numAssets = reader.ReadUInt32();

                while (currAsset++ < numAssets)
                {
                    int length = ValidateRange(reader.ReadInt32(), minValue: 1, maxValue: MaxAssetNameLength);
                    stream.Seek(length, SeekOrigin.Current);
                    uint offset = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();

                    // Check whether the current asset extends past the end of the .pack.temp file.
                    if (end - offset - size < 0 || stream.Seek(sizeof(uint), SeekOrigin.Current) > end)
                    {
                        throw new EndOfStreamException(SR.EndOfStream_AssetFile);
                    }
                }
            }
            while (nextOffset != 0);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or EndOfStreamException)
        {
            // If the current asset info chunk contains at least one valid asset, fix the error by cutting
            // it off at the last valid asset. Otherwise, cut the file off at the previous asset info chunk.
            fix = currAsset > 1 ? new(currOffset, currAsset - 1) : new(prevOffset, 0);
            return true;
        }

        fix = default;
        return false;
    }

    /// <summary>
    /// Renames the specified .pack.temp file to a .pack file.
    /// </summary>
    /// <returns>The renamed .pack file.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static FileInfo RenamePackTempFile(string packTempFile)
    {
        ArgumentNullException.ThrowIfNull(packTempFile);

        FileInfo file = new(packTempFile);
        string name = file.Name;
        string dirName = file.DirectoryName!;
        string newPath;

        // Remove the current file extension.
        if (name.EndsWith(PackTempFileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^PackTempFileSuffix.Length];
        }
        else
        {
            name = name[..^file.Extension.Length];
        }

        // Increment the 3-digit number in the file name if another .pack file is already using it.
        if (name.Length >= 3 && int.TryParse(name[^3..], out int digits))
        {
            name = name[..^3];

            do
            {
                newPath = Path.Combine(dirName, $"{name}{digits++:D3}.pack");
            }
            while (File.Exists(newPath) && !FilesEqual(packTempFile, newPath));
        }
        // If the file name doesn't end with 3 digits, append a digit and increment it if needed.
        else
        {
            digits = 2;
            newPath = Path.Combine(dirName, $"{name}.pack");

            while (File.Exists(newPath) && !FilesEqual(packTempFile, newPath))
            {
                newPath = Path.Combine(dirName, $"{name} ({digits++}).pack");
            }
        }

        file.MoveTo(newPath, overwrite: true);
        return file;
    }

    /// <summary>
    /// Attempts to convert the specified .pack.temp file to a .pack file.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset .temp file was converted; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public static bool TryConvertPackTempFile(string packTempFile, [MaybeNullWhen(false)] out AssetFile assetFile)
    {
        ArgumentNullException.ThrowIfNull(packTempFile);

        if (TryGetPackTempFix(packTempFile, out FixedAssetGroup fix))
        {
            fix.FixPackTempFile(packTempFile);
            assetFile = new AssetFile(RenamePackTempFile(packTempFile).FullName);
            return true;
        }

        assetFile = null;
        return false;
    }

    /// <summary>
    /// Gets an enum value corresponding to the name of the specified asset file.
    /// </summary>
    /// <returns>An enum value corresponding to the name of the specified asset file.</returns>
    /// <exception cref="ArgumentException"/>
    public static AssetType InferAssetType(ReadOnlySpan<char> assetFile,
                                           bool requireFullType = true,
                                           bool strict = false)
    {
        AssetType assetFileType = InferAssetFileType(assetFile, strict);

        if (assetFileType == 0) return 0;

        AssetType assetDirType = InferAssetDirectoryType(assetFile);

        if (!requireFullType || assetDirType != 0)
        {
            return assetFileType | assetDirType;
        }
        if (strict)
        {
            throw new ArgumentException(string.Format(SR.Argument_CantInferAssetType, assetFile.ToString()));
        }

        return 0;
    }

    /// <summary>
    /// Gets an enum value corresponding to the suffix of the specified asset file.
    /// </summary>
    /// <returns>An enum value corresponding to the suffix of the specified asset file.</returns>
    /// <exception cref="ArgumentException"/>
    public static AssetType InferAssetFileType(ReadOnlySpan<char> assetFile, bool strict = false)
    {
        if (assetFile.EndsWith(PackFileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.Pack;
        }
        if (assetFile.EndsWith(ManifestFileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.Dat;
        }
        if (assetFile.EndsWith(PackTempFileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.Pack | AssetType.Temp;
        }
        if (strict)
        {
            throw new ArgumentException(string.Format(SR.Argument_CantInferAssetType, assetFile.ToString()));
        }

        return 0;
    }

    /// <summary>
    /// Gets an enum value corresponding to the prefix of the specified asset file.
    /// </summary>
    /// <returns>An enum value corresponding to the prefix of the specified asset file.</returns>
    /// <exception cref="ArgumentException"/>
    public static AssetType InferAssetDirectoryType(ReadOnlySpan<char> assetFile, bool strict = false)
    {
        ReadOnlySpan<char> filename = Path.GetFileName(assetFile);

        if (GameAssetRegex().IsMatch(filename))
        {
            return AssetType.Game;
        }
        if (TcgAssetRegex().IsMatch(filename))
        {
            return AssetType.Tcg;
        }
        if (ResourceAssetRegex().IsMatch(filename))
        {
            return AssetType.Resource;
        }
        if (PS3AssetRegex().IsMatch(filename))
        {
            return AssetType.PS3;
        }
        if (strict)
        {
            throw new ArgumentException(string.Format(SR.Argument_CantInferAssetType, assetFile.ToString()));
        }

        return 0;
    }

    /// <summary>
    /// Gets an enum value corresponding to the name of the specified asset .dat file.
    /// </summary>
    /// <returns>An enum value corresponding to the name of the specified asset .dat file.</returns>
    /// <exception cref="ArgumentException"/>
    public static AssetType InferDataType(ReadOnlySpan<char> assetDataFile, bool strict = false)
    {
        if (!assetDataFile.EndsWith(DatFileSuffix, StringComparison.OrdinalIgnoreCase)) goto End;

        ReadOnlySpan<char> filename = Path.GetFileName(assetDataFile);

        if (GameDataRegex().IsMatch(filename))
        {
            return AssetType.Game;
        }
        if (TcgDataRegex().IsMatch(filename))
        {
            return AssetType.Tcg;
        }
        if (ResourceDataRegex().IsMatch(filename))
        {
            return AssetType.Resource;
        }
    End:
        if (strict)
        {
            throw new ArgumentException(string.Format(SR.Argument_CantInferAssetType, assetDataFile.ToString()));
        }

        return 0;
    }

    /// <summary>
    /// Throws an exception if the specified integer is outside the given range.
    /// </summary>
    /// <returns>The specified integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    internal static T ValidateRange<T>(T value, T minValue, T maxValue) where T : unmanaged, IBinaryInteger<T>
    {
        if (minValue <= value && value <= maxValue)
        {
            return value;
        }
        else
        {
            string message = string.Format(SR.ArgumentOutOfRange_Integer, value, minValue, maxValue);
            ArgumentOutOfRangeException exception = new(message, innerException: null);
            exception.Data["BytesRead"] = Unsafe.SizeOf<T>();
            throw exception;
        }
    }

    /// <summary>
    /// Writes the specified list of assets to the asset chunk at the given offset.
    /// </summary>
    private static void WritePackAssetChunk(EndianBinaryWriter writer,
                                            List<FileStream> assets,
                                            uint chunkOffset,
                                            uint assetOffset,
                                            byte[] buffer)
    {
        // Write the offset of the next asset chunk and the number of assets in this chunk.
        writer.Write(chunkOffset);
        writer.Write(assets.Count);
        assetOffset += SizeOfPackAssetChunkHeader;

        // Create an index of each asset in the asset info chunk.
        foreach (FileStream asset in assets)
        {
            uint size = (uint)asset.Length;
            writer.Write(Path.GetFileName(asset.Name)); // Name
            writer.Write(assetOffset);                  // Offset
            writer.Write(size);                         // Size
            writer.Write(GetCrc32(asset, buffer));      // CRC-32
            assetOffset += size;
        }

        // Copy each asset into the asset content chunk.
        foreach (FileStream asset in assets)
        {
            asset.CopyTo(writer.BaseStream);
            asset.Dispose();
        }

        // Clear the asset list.
        assets.Clear();
    }

    /// <summary>
    /// Computes the CRC-32 of the bytes in the specified stream.
    /// </summary>
    /// <returns>The CRC-32 checksum value of the specified stream.</returns>
    private static uint GetCrc32(Stream stream, byte[] buffer)
    {
        uint crc32 = 0u;
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            crc32 = Crc32Algorithm.Append(crc32, buffer, 0, bytesRead);
        }

        stream.Position = 0;
        return crc32;
    }

    /// <summary>
    /// Checks whether the contents of the files are the same.
    /// </summary>
    /// <returns><see langword="true"/> if the files have the same bytes; otherwise, <see langword="false"/>.</returns>
    private static bool FilesEqual(string file1, string file2)
    {
        if (file1 == file2) return true;

        using FileStream stream1 = File.OpenRead(file1);
        using FileStream stream2 = File.OpenRead(file2);

        if (stream1.Length != stream2.Length) return false;

        byte[] buffer1 = ArrayPool<byte>.Shared.Rent(BufferSize);
        byte[] buffer2 = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            int bytesRead1, bytesRead2;

            do
            {
                bytesRead1 = stream1.Read(buffer1);
                bytesRead2 = stream2.Read(buffer2);

                if (!buffer1.AsSpan(0, bytesRead1).SequenceEqual(buffer2.AsSpan(0, bytesRead2))) return false;
            }
            while (bytesRead1 != 0);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer1);
            ArrayPool<byte>.Shared.Return(buffer2);
        }

        return true;
    }
}

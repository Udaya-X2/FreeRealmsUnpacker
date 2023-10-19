using System.Text.RegularExpressions;
using AssetIO.EndianBinaryIO;

namespace AssetIO;

/// <summary>
/// Provides static methods for obtaining asset information from a Free Realms client file.
/// </summary>
public static partial class ClientFile
{
    private const int ManifestChunkSize = 148;
    private const int MaxAssetNameLength = 128;
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

    [GeneratedRegex("^Assets((_ps3)?W?_\\d{3}\\.pack|_manifest\\.dat)$", Options, "en-US")]
    private static partial Regex GameAssetRegex();
    [GeneratedRegex("^assetpack000(W?_\\d{3}\\.pack|_manifest\\.dat)$", Options, "en-US")]
    private static partial Regex TcgAssetRegex();
    [GeneratedRegex("^AssetsTcg(W?_\\d{3}\\.pack|_manifest\\.dat)$", Options, "en-US")]
    private static partial Regex ResourceAssetRegex();

    /// <summary>
    /// Returns an enumerable collection of the assets in the specified file.
    /// </summary>
    /// <returns>An enumerable collection of the assets in the specified file.</returns>
    /// <exception cref="ArgumentException"/>
    public static IEnumerable<Asset> EnumerateAssets(string assetFile) => InferAssetFileType(assetFile) switch
    {
        AssetType.Dat => EnumerateManifestAssets(assetFile),
        AssetType.Pack => EnumeratePackAssets(assetFile),
        _ => throw new ArgumentException(string.Format(SR.Argument_UnkAssetExt, assetFile), nameof(assetFile))
    };

    /// <summary>
    /// Returns the assets in the specified file.
    /// </summary>
    /// <returns>An array consisting of the assets in the specified file.</returns>
    /// <exception cref="ArgumentException"/>
    public static Asset[] GetAssets(string assetFile) => InferAssetFileType(assetFile) switch
    {
        AssetType.Dat => GetManifestAssets(assetFile),
        AssetType.Pack => GetPackAssets(assetFile),
        _ => throw new ArgumentException(string.Format(SR.Argument_UnkAssetExt, assetFile), nameof(assetFile))
    };

    /// <summary>
    /// Returns the number of assets in the specified file.
    /// </summary>
    /// <returns>The number of assets in the specified file.</returns>
    /// <exception cref="ArgumentException"/>
    public static int GetAssetCount(string assetFile) => InferAssetFileType(assetFile) switch
    {
        AssetType.Dat => GetManifestAssetCount(assetFile),
        AssetType.Pack => GetPackAssetCount(assetFile),
        _ => throw new ArgumentException(string.Format(SR.Argument_UnkAssetExt, assetFile), nameof(assetFile))
    };

    /// <summary>
    /// Returns an enumerable collection of the assets in the specified manifest.dat file.
    /// </summary>
    /// <returns>An enumerable collection of the assets in the specified manifest.dat file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static IEnumerable<Asset> EnumerateManifestAssets(string manifestFile)
    {
        if (manifestFile == null) throw new ArgumentNullException(nameof(manifestFile));

        // Read the manifest.dat file in little-endian format.
        using FileStream stream = File.OpenRead(manifestFile);
        using EndianBinaryReader reader = new(stream, Endian.Little);
        int numAssets = (int)(stream.Length / ManifestChunkSize);
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
                long offset = ValidateRange(reader.ReadInt64(), minValue: 0);
                uint size = reader.ReadUInt32();
                uint crc32 = reader.ReadUInt32();
                asset = new Asset(name, offset, size, crc32);
                stream.Seek(MaxAssetNameLength - length, SeekOrigin.Current);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new IOException(string.Format(SR.IO_BadAsset, stream.Position, stream.Name), ex);
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
        if (manifestFile == null) throw new ArgumentNullException(nameof(manifestFile));

        Asset[] assets = new Asset[GetManifestAssetCount(manifestFile)];
        int index = 0;

        foreach (Asset asset in EnumerateManifestAssets(manifestFile))
        {
            assets[index++] = asset;
        }

        return assets;
    }

    /// <summary>
    /// Returns the number of assets in the specified manifest.dat file.
    /// </summary>
    /// <returns>The number of assets in the specified manifest.dat file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static int GetManifestAssetCount(string manifestFile)
    {
        if (manifestFile == null) throw new ArgumentNullException(nameof(manifestFile));

        FileInfo manifestFileInfo = new(manifestFile);

        return manifestFileInfo.Length % ManifestChunkSize == 0
            ? (int)(manifestFileInfo.Length / ManifestChunkSize)
            : throw new IOException(string.Format(SR.IO_BadManifest, manifestFile));
    }

    /// <summary>
    /// Returns an enumerable collection of the assets in the specified .pack file.
    /// </summary>
    /// <returns>An enumerable collection of the assets in the specified .pack file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static IEnumerable<Asset> EnumeratePackAssets(string packFile)
    {
        if (packFile == null) throw new ArgumentNullException(nameof(packFile));

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
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new IOException(string.Format(SR.IO_BadAsset, stream.Position, stream.Name), ex);
                }
                catch (EndOfStreamException ex)
                {
                    throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, stream.Name), ex);
                }

                yield return asset;
            }
        } while (nextOffset != 0);
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
        : EnumeratePackAssets(packFile).ToArray();

    /// <summary>
    /// Returns the number of assets in the specified .pack file.
    /// </summary>
    /// <returns>The number of assets in the specified .pack file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static int GetPackAssetCount(string packFile)
    {
        if (packFile == null) throw new ArgumentNullException(nameof(packFile));

        // Read the .pack file in big-endian format.
        using FileStream stream = new(packFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8);
        using EndianBinaryReader reader = new(stream, Endian.Big);
        uint nextOffset = 0;
        uint numAssets = 0;

        // Read the number of assets in each asset info chunk.
        try
        {
            do
            {
                stream.Position = nextOffset;
                nextOffset = reader.ReadUInt32();
                numAssets += reader.ReadUInt32();
            } while (nextOffset != 0);
        }
        catch (EndOfStreamException ex)
        {
            throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, stream.Name), ex);
        }

        return (int)numAssets;
    }

    /// <summary>
    /// Gets an enum value corresponding to the name of the specified asset file.
    /// </summary>
    /// <returns>An enum value corresponding to the name of the specified asset file.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static AssetType InferAssetType(string assetFile)
    {
        if (assetFile == null) throw new ArgumentNullException(nameof(assetFile));

        AssetType assetFileType = InferAssetFileType(assetFile);

        if (assetFileType == 0) return 0;

        AssetType assetDirType = InferAssetDirectoryType(assetFile);

        return assetDirType == 0 ? 0 : assetFileType | assetDirType;
    }

    /// <summary>
    /// Gets an enum value corresponding to the suffix of the specified asset file.
    /// </summary>
    /// <returns>An enum value corresponding to the suffix of the specified asset file.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static AssetType InferAssetFileType(string assetFile)
    {
        if (assetFile == null) throw new ArgumentNullException(nameof(assetFile));

        if (assetFile.EndsWith(".pack", StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.Pack;
        }
        if (assetFile.EndsWith("_manifest.dat", StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.Dat;
        }

        return 0;
    }

    /// <summary>
    /// Gets an enum value corresponding to the prefix of the specified asset file.
    /// </summary>
    /// <returns>An enum value corresponding to the prefix of the specified asset file.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static AssetType InferAssetDirectoryType(string assetFile)
    {
        if (assetFile == null) throw new ArgumentNullException(nameof(assetFile));

        assetFile = Path.GetFileName(assetFile);

        if (GameAssetRegex().IsMatch(assetFile))
        {
            return AssetType.Game;
        }
        if (TcgAssetRegex().IsMatch(assetFile))
        {
            return AssetType.Tcg;
        }
        if (ResourceAssetRegex().IsMatch(assetFile))
        {
            return AssetType.Resource;
        }

        return 0;
    }

    /// <summary>
    /// Throws an exception if the specified 32-bit integer is outside the given range.
    /// </summary>
    /// <returns>The specified 32-bit integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    private static int ValidateRange(int n, int minValue = int.MinValue, int maxValue = int.MaxValue)
    {
        return minValue <= n && n <= maxValue ? n : throw new ArgumentOutOfRangeException(nameof(n));
    }

    /// <summary>
    /// Throws an exception if the specified 64-bit integer is outside the given range.
    /// </summary>
    /// <returns>The specified 64-bit integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    private static long ValidateRange(long n, long minValue = long.MinValue, long maxValue = long.MaxValue)
    {
        return minValue <= n && n <= maxValue ? n : throw new ArgumentOutOfRangeException(nameof(n));
    }
}

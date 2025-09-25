using AssetIO.EndianBinaryIO;
using Microsoft.Win32.SafeHandles;
using System.Buffers;

namespace AssetIO;

/// <summary>
/// Provides static methods for the reading, writing, extraction,
/// and validation of assets from a Free Realms asset file.
/// </summary>
public static class ClientFile
{
    /// <summary>
    /// Returns an enumerable collection of the assets in the specified .pack file.
    /// </summary>
    /// <param name="path">The asset .pack file to read.</param>
    /// <returns>An enumerable collection of the assets in the specified .pack file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static IEnumerable<Asset> EnumeratePackAssets(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Read the .pack file in big-endian format.
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.PackChunkSize);
        EndianBinaryReader reader = new(stream, Endian.Big, Constants.MaxAssetNameLength);
        uint nextOffset = 0;
        uint numAssets = 0;
        Asset? asset = null;

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
                ThrowHelper.ThrowEndOfStream_AssetFile(stream.Name, ex);
            }

            for (uint i = 0; i < numAssets; i++)
            {
                try
                {
                    int length = ValidateNameLength(reader.ReadInt32());
                    string name = reader.ReadString(length);
                    uint offset = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();
                    uint crc32 = reader.ReadUInt32();
                    asset = new Asset(name, offset, size, crc32);
                }
                catch (InvalidAssetException ex)
                {
                    ThrowHelper.ThrowIO_BadAsset(stream.Position - ex.Size, stream.Name, ex);
                }
                catch (EndOfStreamException ex)
                {
                    ThrowHelper.ThrowEndOfStream_AssetFile(stream.Name, ex);
                }

                yield return asset;
            }
        }
        while (nextOffset != 0);
    }

    /// <summary>
    /// Returns the assets in the specified .pack file.
    /// </summary>
    /// <param name="path">The asset .pack file to read.</param>
    /// <returns>An array consisting of the assets in the specified .pack file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OutOfMemoryException"/>
    public static Asset[] GetPackAssets(string path)
        => [.. EnumeratePackAssets(path)];

    /// <summary>
    /// Returns the number of assets in the specified .pack file.
    /// </summary>
    /// <param name="path">The asset .pack file to read.</param>
    /// <returns>The number of assets in the specified .pack file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OverflowException"/>
    public static int GetPackAssetCount(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Read the .pack file in big-endian format.
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.PackHeaderSize);
        EndianBinaryReader reader = new(stream, Endian.Big);
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
            return ThrowHelper.ThrowEndOfStream_AssetFile<int>(stream.Name, ex);
        }
        catch (OverflowException ex)
        {
            return ThrowHelper.ThrowOverflow_TooManyAssets<int>(stream.Name, ex);
        }
    }

    /// <summary>
    /// Opens a .pack file, appends the specified assets to the file, and then closes
    /// the file. If the target file does not exist, this method creates the file.
    /// </summary>
    /// <param name="path">The asset .pack file to append.</param>
    /// <param name="assets">The files to append as assets to the .pack file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static void AppendPackAssets(string path, IEnumerable<string> assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(assets);

        using AssetPackWriter writer = new(path, append: true);

        foreach (string asset in assets)
        {
            writer.Write(asset);
        }
    }

    /// <summary>
    /// Creates a new .pack file, writes the specified assets to the file, and then
    /// closes the file. If the target file already exists, it is overwritten.
    /// </summary>
    /// <param name="path">The asset .pack file to write.</param>
    /// <param name="assets">The files to write as assets to the .pack file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static void WritePackAssets(string path, IEnumerable<string> assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(assets);

        using AssetPackWriter writer = new(path);

        foreach (string asset in assets)
        {
            writer.Write(asset);
        }
    }

    /// <inheritdoc cref="RemovePackAssets(string, ISet{Asset})"/>
    public static void RemovePackAssets(string path, IEnumerable<Asset> assets)
        => RemovePackAssets(path, assets.ToHashSet());

    /// <summary>
    /// Removes the specified assets from the given .pack file.
    /// </summary>
    /// <param name="path">The asset .pack file to modify.</param>
    /// <param name="assets">The assets to remove from the .pack file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static void RemovePackAssets(string path, ISet<Asset> assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(assets);

        string tempFile = GetTempFileName(path);

        try
        {
            using (AssetPackReader reader = new(path))
            using (AssetPackWriter writer = new(tempFile))
            {
                // Copy the non-removed assets to a temp .pack file.
                foreach (Asset asset in EnumeratePackAssets(path))
                {
                    if (!assets.Contains(asset))
                    {
                        reader.CopyTo(asset, writer);
                    }
                }
            }

            // Replace the original .pack file with the temp .pack file.
            File.Move(tempFile, path, overwrite: true);
        }
        catch
        {
            // Attempt to delete the temp .pack file if an error occurs.
            TryDeleteFiles(tempFile);
            throw;
        }
    }

    /// <summary>
    /// Throws an exception if the specified .pack file is invalid or contains assets with CRC-32 mismatches.
    /// </summary>
    /// <param name="path">The asset .pack file to validate.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static void ValidatePackAssets(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        using AssetPackReader reader = new(path);

        foreach (Asset asset in EnumeratePackAssets(path))
        {
            uint crc32 = reader.GetCrc32(asset);

            if (asset.Crc32 != crc32) ThrowHelper.ThrowIO_CrcMismatch(asset.Name, crc32, asset.Crc32, path);
        }
    }

    /// <summary>
    /// Extracts the assets from the specified .pack file to the given directory.
    /// </summary>
    /// <param name="path">The asset .pack file to extract.</param>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="options">Specifies how to handle file conflicts in the destination path.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public static void ExtractPackAssets(string path, string destDir, FileConflictOptions options = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(destDir);

        using AssetPackReader reader = new(path);

        foreach (Asset asset in EnumeratePackAssets(path))
        {
            reader.ExtractTo(asset, destDir, options);
        }
    }

    /// <summary>
    /// Returns an enumerable collection of the assets in the specified manifest.dat file.
    /// </summary>
    /// <param name="path">The manifest.dat file to read.</param>
    /// <returns>An enumerable collection of the assets in the specified manifest.dat file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static IEnumerable<Asset> EnumerateManifestAssets(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Read the manifest.dat file in little-endian format.
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.BufferSize);
        EndianBinaryReader reader = new(stream, Endian.Little, Constants.MaxAssetNameLength);
        (long numAssets, long remainder) = Math.DivRem(stream.Length, Constants.ManifestChunkSize);
        Asset? asset = null;

        if (remainder != 0) ThrowHelper.ThrowIO_BadManifest(stream.Name);

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
        for (long i = 0; i < numAssets; i++)
        {
            try
            {
                int length = ValidateNameLength(reader.ReadInt32());
                string name = reader.ReadString(length);
                long offset = ValidateOffset(reader.ReadInt64());
                uint size = reader.ReadUInt32();
                uint crc32 = reader.ReadUInt32();
                asset = new Asset(name, offset, size, crc32);
                stream.Seek(Constants.MaxAssetNameLength - length, SeekOrigin.Current);
            }
            catch (InvalidAssetException ex)
            {
                ThrowHelper.ThrowIO_BadAsset(stream.Position - ex.Size, stream.Name, ex);
            }

            yield return asset;
        }
    }

    /// <summary>
    /// Returns the assets in the specified manifest.dat file.
    /// </summary>
    /// <param name="path">The manifest.dat file to read.</param>
    /// <returns>An array consisting of the assets in the specified manifest.dat file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OutOfMemoryException"/>
    public static Asset[] GetManifestAssets(string path)
        => [.. EnumerateManifestAssets(path)];

    /// <summary>
    /// Returns the number of assets in the specified manifest.dat file.
    /// </summary>
    /// <param name="path">The manifest.dat file to read.</param>
    /// <returns>The number of assets in the specified manifest.dat file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OverflowException"/>
    public static int GetManifestAssetCount(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        FileInfo file = new(path);
        (long numAssets, long remainder) = Math.DivRem(file.Length, Constants.ManifestChunkSize);

        if (remainder != 0) ThrowHelper.ThrowIO_BadManifest(path);
        if (numAssets > int.MaxValue) ThrowHelper.ThrowOverflow_TooManyAssets(file.Name);

        return (int)numAssets;
    }

    /// <summary>
    /// Opens a manifest.dat file, appends the specified assets to the file, and then
    /// closes the file. If the target file does not exist, this method creates the file.
    /// </summary>
    /// <param name="path">The manifest.dat file to append.</param>
    /// <param name="assets">The files to append as assets to the manifest.dat file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static void AppendManifestAssets(string path, IEnumerable<string> assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(assets);

        using AssetDatWriter writer = new(path, append: true);

        foreach (string asset in assets)
        {
            writer.Write(asset);
        }
    }

    /// <summary>
    /// Creates a new manifest.dat file, writes the specified assets to the file, and
    /// then closes the file. If the target file already exists, it is overwritten.
    /// </summary>
    /// <param name="path">The manifest.dat file to write.</param>
    /// <param name="assets">The files to write as assets to the manifest.dat file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static void WriteManifestAssets(string path, IEnumerable<string> assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(assets);

        using AssetDatWriter writer = new(path);

        foreach (string asset in assets)
        {
            writer.Write(asset);
        }
    }

    /// <summary>
    /// Extracts the assets from the asset .dat files corresponding
    /// to the specified manifest.dat file to the given directory.
    /// </summary>
    /// <param name="path">The manifest.dat file to extract.</param>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="options">Specifies how to handle file conflicts in the destination path.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="IOException"/>
    public static void ExtractManifestAssets(string path, string destDir, FileConflictOptions options = default)
        => ExtractManifestAssets(path, ClientDirectory.EnumerateDataFiles(path), destDir, options);

    /// <summary>
    /// Extracts the assets from the given asset .dat files to the
    /// given directory, using the specified manifest.dat file.
    /// </summary>
    /// <param name="path">The manifest.dat file to extract.</param>
    /// <param name="dataFiles">The asset .dat files to extract.</param>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="options">Specifies how to handle file conflicts in the destination path.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="IOException"/>
    public static void ExtractManifestAssets(string path,
                                             IEnumerable<string> dataFiles,
                                             string destDir,
                                             FileConflictOptions options = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(destDir);

        using AssetDatReader reader = new(dataFiles);

        foreach (Asset asset in EnumerateManifestAssets(path))
        {
            reader.ExtractTo(asset, destDir, options);
        }
    }

    /// <inheritdoc cref="RemoveManifestAssets(string, IEnumerable{string}, ISet{Asset})"/>
    public static void RemoveManifestAssets(string path, IEnumerable<Asset> assets)
        => RemoveManifestAssets(path, ClientDirectory.EnumerateDataFiles(path), assets.ToHashSet());

    /// <inheritdoc cref="RemoveManifestAssets(string, IEnumerable{string}, ISet{Asset})"/>
    public static void RemoveManifestAssets(string path, ISet<Asset> assets)
        => RemoveManifestAssets(path, ClientDirectory.EnumerateDataFiles(path), assets);

    /// <inheritdoc cref="RemoveManifestAssets(string, IEnumerable{string}, ISet{Asset})"/>
    public static void RemoveManifestAssets(string path, IEnumerable<string> dataFiles, IEnumerable<Asset> assets)
        => RemoveManifestAssets(path, dataFiles, assets.ToHashSet());

    /// <summary>
    /// Removes the specified assets from the given manifest.dat file.
    /// </summary>
    /// <param name="path">The manifest.dat file to modify.</param>
    /// <param name="dataFiles">The asset .dat files to modify.</param>
    /// <param name="assets">The assets to remove from the manifest.dat file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static void RemoveManifestAssets(string path, IEnumerable<string> dataFiles, ISet<Asset> assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(dataFiles);
        ArgumentNullException.ThrowIfNull(assets);

        string tempFile = GetTempFileName(path);

        try
        {
            using (AssetDatReader reader = new(dataFiles))
            using (AssetDatWriter writer = new(tempFile))
            {
                // Copy the non-removed assets to temp manifest.dat and asset .dat files.
                foreach (Asset asset in EnumerateManifestAssets(path))
                {
                    if (!assets.Contains(asset))
                    {
                        reader.CopyTo(asset, writer);
                    }
                }
            }

            // Replace the original manifest.dat file with the temp manifest.dat file.
            File.Move(tempFile, path, overwrite: true);
            using IEnumerator<string> dataFileEnumerator = dataFiles.GetEnumerator();

            // Replace the original asset .dat files with the temp asset .dat files.
            foreach (string tempDataFile in ClientDirectory.EnumerateDataFiles(tempFile))
            {
                dataFileEnumerator.MoveNext();
                string dataFile = dataFileEnumerator.Current;
                File.Move(tempDataFile, dataFile, overwrite: true);
            }

            // Delete any unused asset .dat files.
            while (dataFileEnumerator.MoveNext() && File.Exists(dataFileEnumerator.Current))
            {
                File.Delete(dataFileEnumerator.Current);
            }
        }
        catch
        {
            // Attempt to delete the temp files if an error occurs.
            TryDeleteFiles(ClientDirectory.EnumerateDataFiles(tempFile).Prepend(tempFile));
            throw;
        }
    }

    /// <summary>
    /// Throws an exception if the specified manifest.dat file and corresponding
    /// asset .dat files are invalid or contain assets with CRC-32 mismatches.
    /// </summary>
    /// <param name="path">The manifest.dat file to validate.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static void ValidateManifestAssets(string path)
        => ValidateManifestAssets(path, ClientDirectory.EnumerateDataFiles(path));

    /// <summary>
    /// Throws an exception if the specified manifest.dat file and asset
    /// .dat files are invalid or contain assets with CRC-32 mismatches.
    /// </summary>
    /// <param name="path">The manifest.dat file to validate.</param>
    /// <param name="dataFiles">The asset .dat files to validate.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static void ValidateManifestAssets(string path, IEnumerable<string> dataFiles)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(dataFiles);

        using AssetDatReader reader = new(dataFiles);

        foreach (Asset asset in EnumerateManifestAssets(path))
        {
            uint crc32 = reader.GetCrc32(asset);

            if (asset.Crc32 != crc32) ThrowHelper.ThrowIO_CrcMismatch(asset.Name, crc32, asset.Crc32, path);
        }
    }

    /// <summary>
    /// Returns an enumerable collection of the valid assets in the specified .pack.temp file.
    /// </summary>
    /// <param name="path">The asset .pack.temp file to read.</param>
    /// <returns>An enumerable collection of the valid assets in the specified .pack.temp file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static IEnumerable<Asset> EnumeratePackTempAssets(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        using IEnumerator<Asset> enumerator = EnumeratePackAssets(path).GetEnumerator();
        long end = new FileInfo(path).Length;

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
            catch (Exception ex) when (ex is EndOfStreamException or { InnerException: InvalidAssetException })
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
    /// <param name="path">The asset .pack.temp file to read.</param>
    /// <returns>Gets the valid assets in the specified .pack.temp file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OutOfMemoryException"/>
    public static Asset[] GetPackTempAssets(string path)
        => [.. EnumeratePackTempAssets(path)];

    /// <summary>
    /// Returns the number of the valid assets in the specified .pack.temp file.
    /// </summary>
    /// <param name="path">The asset .pack.temp file to read.</param>
    /// <returns>An number of the valid assets in the specified .pack.temp file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OverflowException"/>
    public static int GetPackTempAssetCount(string path)
        => EnumeratePackTempAssets(path).Count();

    /// <summary>
    /// Extracts the assets from the specified .pack.temp file to the given directory.
    /// </summary>
    /// <param name="path">The asset .pack.temp file to extract.</param>
    /// <param name="destDir">The destination directory path.</param>
    /// <param name="options">Specifies how to handle file conflicts in the destination path.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="IOException"/>
    public static void ExtractPackTempAssets(string path, string destDir, FileConflictOptions options = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(destDir);

        using AssetPackReader reader = new(path);

        foreach (Asset asset in EnumeratePackTempAssets(path))
        {
            reader.ExtractTo(asset, destDir, options);
        }
    }

    /// <summary>
    /// Fixes the first invalid asset info chunk in the specified .pack.temp file.
    /// </summary>
    /// <param name="path">The asset .pack.temp file to fix.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static void FixPackTempFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (TryGetPackTempFix(path, out FixedAssetChunk fix))
        {
            fix.FixPackTempFile(path);
        }
    }

    /// <summary>
    /// Attempts to scan the specified .pack.temp file for errors
    /// and create a fix for the first invalid asset info chunk.
    /// </summary>
    /// <param name="path">The asset .pack.temp file to fix.</param>
    /// <param name="fix">On return, contains a fix for the .pack.temp file or an undefined value on failure.</param>
    /// <returns>
    /// <see langword="true"/> if the .pack.temp file contains a fixable error; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static bool TryGetPackTempFix(string path, out FixedAssetChunk fix)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Read the .pack.temp file in big-endian format.
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, Constants.PackChunkSize);
        EndianBinaryReader reader = new(stream, Endian.Big);
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
                    int length = ValidateNameLength(reader.ReadInt32());
                    stream.Seek(length, SeekOrigin.Current);
                    uint offset = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();

                    // Check whether the current asset extends past the end of the .pack.temp file.
                    if (end - offset - size < 0 || stream.Seek(sizeof(uint), SeekOrigin.Current) > end)
                    {
                        ThrowHelper.ThrowEndOfStream_AssetFile(stream.Name);
                    }
                }
            }
            while (nextOffset != 0);
        }
        catch (Exception ex) when (ex is EndOfStreamException or InvalidAssetException)
        {
            // If the current asset info chunk contains at least one valid asset, fix the error by cutting
            // it off at the last valid asset. Otherwise, cut the file off at the previous asset info chunk.
            fix = currAsset > 1 ? new(currOffset, currAsset - 1) : new(prevOffset, 0);
            return true;
        }

        fix = default;
        return false;
    }

    /// <inheritdoc cref="RenamePackTempFile(FileInfo)"/>
    public static FileInfo RenamePackTempFile(string file)
        => RenamePackTempFile(new FileInfo(file));

    /// <summary>
    /// Renames the specified .pack.temp file to a .pack file.
    /// </summary>
    /// <param name="file">The asset .pack.temp file to rename.</param>
    /// <returns>The renamed .pack file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static FileInfo RenamePackTempFile(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        string name = file.Name;
        string dirName = file.DirectoryName!;
        string newPath;

        // Remove the current file extension.
        if (name.EndsWith(Constants.PackTempFileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^Constants.PackTempFileSuffix.Length];
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
            while (File.Exists(newPath) && !FilesEqual(file.FullName, newPath));
        }
        // If the file name doesn't end with 3 digits, append a digit and increment it if needed.
        else
        {
            digits = 2;
            newPath = Path.Combine(dirName, $"{name}.pack");

            while (File.Exists(newPath) && !FilesEqual(file.FullName, newPath))
            {
                newPath = Path.Combine(dirName, $"{name} ({digits++}).pack");
            }
        }

        file.MoveTo(newPath, overwrite: true);
        return file;
    }

    /// <inheritdoc cref="ConvertPackTempFile(FileInfo)"/>
    public static AssetFile ConvertPackTempFile(AssetFile file)
        => new(ConvertPackTempFile(file.FullName).FullName, file.Type & ~AssetType.Temp);

    /// <inheritdoc cref="ConvertPackTempFile(FileInfo)"/>
    public static FileInfo ConvertPackTempFile(string file)
        => ConvertPackTempFile(new FileInfo(file));

    /// <summary>
    /// Converts the specified .pack.temp file to a .pack file.
    /// </summary>
    /// <param name="file">The asset .pack.temp file to convert.</param>
    /// <returns>The converted .pack file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static FileInfo ConvertPackTempFile(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        FixPackTempFile(file.FullName);
        return RenamePackTempFile(file);
    }

    /// <summary>
    /// Throws an exception if the specified .pack.temp file is invalid or contains assets with CRC-32 mismatches.
    /// </summary>
    /// <param name="path">The asset .pack.temp file to validate.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public static void ValidatePackTempAssets(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        using AssetPackReader reader = new(path);

        foreach (Asset asset in EnumeratePackTempAssets(path))
        {
            uint crc32 = reader.GetCrc32(asset);

            if (asset.Crc32 != crc32) ThrowHelper.ThrowIO_CrcMismatch(asset.Name, crc32, asset.Crc32, path);
        }
    }

    /// <summary>
    /// Gets an enum value corresponding to the name of the specified asset file.
    /// </summary>
    /// <remarks>
    /// It is recommended to keep <paramref name="requireFullType"/> = <see langword="true"/> to
    /// exclude non Free Realms files, such as the .pack files used by Git in <c>.git/objects/pack/</c>.
    /// </remarks>
    /// <param name="path">A read-only span containing the name of the asset file.</param>
    /// <param name="requireFullType">
    /// Whether to return 0 if both a file flag and a directory flag cannot be inferred.
    /// </param>
    /// <param name="strict">Whether to throw an exception if an asset type cannot be inferred.</param>
    /// <returns>An enum value corresponding to the name of the specified asset file.</returns>
    /// <exception cref="ArgumentException"/>
    public static AssetType InferAssetType(ReadOnlySpan<char> path, bool requireFullType = true, bool strict = false)
    {
        AssetType assetFileType = InferAssetFileType(path, strict);

        if (assetFileType == 0) return 0;

        AssetType assetDirType = InferAssetDirectoryType(path);

        if (!requireFullType || assetDirType != 0) return assetFileType | assetDirType;
        if (strict) ThrowHelper.ThrowArgument_CantInferAssetType(path.ToString());
        return 0;
    }

    /// <summary>
    /// Gets an enum value corresponding to the suffix of the specified asset file.
    /// </summary>
    /// <param name="path">A read-only span containing the name of the asset file.</param>
    /// <param name="strict">Whether to throw an exception if an asset type cannot be inferred.</param>
    /// <returns>An enum value corresponding to the suffix of the specified asset file.</returns>
    /// <exception cref="ArgumentException"/>
    public static AssetType InferAssetFileType(ReadOnlySpan<char> path, bool strict = false)
    {
        const StringComparison ComparisonType = StringComparison.OrdinalIgnoreCase;

        if (path.EndsWith(Constants.PackFileSuffix, ComparisonType)) return AssetType.Pack;
        if (path.EndsWith(Constants.ManifestFileSuffix, ComparisonType)) return AssetType.Dat;
        if (path.EndsWith(Constants.PackTempFileSuffix, ComparisonType)) return AssetType.Pack | AssetType.Temp;
        if (strict) ThrowHelper.ThrowArgument_CantInferAssetType(path.ToString());
        return 0;
    }

    /// <summary>
    /// Gets an enum value corresponding to the prefix of the specified asset file.
    /// </summary>
    /// <param name="path">A read-only span containing the name of the asset file.</param>
    /// <param name="strict">Whether to throw an exception if an asset type cannot be inferred.</param>
    /// <returns>An enum value corresponding to the prefix of the specified asset file.</returns>
    /// <exception cref="ArgumentException"/>
    public static AssetType InferAssetDirectoryType(ReadOnlySpan<char> path, bool strict = false)
    {
        ReadOnlySpan<char> fileName = Path.GetFileName(path);

        if (RegexConstants.GameAssetRegex.IsMatch(fileName)) return AssetType.Game;
        if (RegexConstants.TcgAssetRegex.IsMatch(fileName)) return AssetType.Tcg;
        if (RegexConstants.ResourceAssetRegex.IsMatch(fileName)) return AssetType.Resource;
        if (RegexConstants.PS3AssetRegex.IsMatch(fileName)) return AssetType.PS3;
        if (strict) ThrowHelper.ThrowArgument_CantInferAssetType(path.ToString());
        return 0;
    }

    /// <summary>
    /// Gets an enum value corresponding to the name of the specified asset .dat file.
    /// </summary>
    /// <param name="path">A read-only span containing the name of the asset .dat file.</param>
    /// <param name="strict">Whether to throw an exception if an asset type cannot be inferred.</param>
    /// <returns>An enum value corresponding to the name of the specified asset .dat file.</returns>
    /// <exception cref="ArgumentException"/>
    public static AssetType InferDataType(ReadOnlySpan<char> path, bool strict = false)
    {
        if (!path.EndsWith(Constants.DatFileSuffix, StringComparison.OrdinalIgnoreCase)) goto End;

        ReadOnlySpan<char> filename = Path.GetFileName(path);

        if (RegexConstants.GameDataRegex.IsMatch(filename)) return AssetType.Game;
        if (RegexConstants.TcgDataRegex.IsMatch(filename)) return AssetType.Tcg;
        if (RegexConstants.ResourceDataRegex.IsMatch(filename)) return AssetType.Resource;
        End:
        if (strict) ThrowHelper.ThrowArgument_CantInferAssetType(path.ToString());

        return 0;
    }

    /// <summary>
    /// Throws an exception if the specified integer is an invalid asset name length.
    /// </summary>
    /// <param name="value">The integer value to check.</param>
    /// <returns>The specified integer value.</returns>
    /// <exception cref="InvalidAssetException"/>
    internal static int ValidateNameLength(int value)
        => value is >= 1 and <= Constants.MaxAssetNameLength ? value : ThrowHelper.ThrowInvalidAsset_Name(value);

    /// <summary>
    /// Throws an exception if the specified integer is an invalid asset offset.
    /// </summary>
    /// <param name="value">The integer value to check.</param>
    /// <returns>The specified integer.</returns>
    /// <exception cref="InvalidAssetException"/>
    private static long ValidateOffset(long value)
        => value >= 0 ? value : ThrowHelper.ThrowInvalidAsset_Offset(value);

    /// <summary>
    /// Checks whether the contents of the specified files are the same.
    /// </summary>
    /// <returns><see langword="true"/> if the files have the same bytes; otherwise, <see langword="false"/>.</returns>
    private static bool FilesEqual(string path1, string path2)
    {
        if (path1 == path2) return true;

        using SafeFileHandle handle1 = File.OpenHandle(path1, options: FileOptions.SequentialScan);
        using SafeFileHandle handle2 = File.OpenHandle(path2, options: FileOptions.SequentialScan);
        long length = RandomAccess.GetLength(handle1);

        if (length != RandomAccess.GetLength(handle2)) return false;

        byte[] buffer1 = ArrayPool<byte>.Shared.Rent(Constants.BufferSize);
        byte[] buffer2 = ArrayPool<byte>.Shared.Rent(Constants.BufferSize);

        try
        {
            Span<byte> span1 = buffer1.AsSpan(0, Constants.BufferSize);
            Span<byte> span2 = buffer2.AsSpan(0, Constants.BufferSize);
            long position = 0;

            while (true)
            {
                int read = RandomAccess.Read(handle1, span1, position);

                if (read == 0) return position == length;
                if (!ReadExactly(handle2, span2, position, read)) return false;
                if (!span1[..read].SequenceEqual(span2[..read])) return false;

                position += read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer1);
            ArrayPool<byte>.Shared.Return(buffer2);
        }
    }

    /// <summary>
    /// Reads the specified number of bytes from the given file at the given offset into the buffer.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if exactly <paramref name="numBytes"/> bytes
    /// were read into the buffer; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool ReadExactly(SafeFileHandle handle, Span<byte> buffer, long fileOffset, int numBytes)
    {
        int totalRead = 0;

        while (totalRead < numBytes)
        {
            int read = RandomAccess.Read(handle, buffer[totalRead..], fileOffset + totalRead);

            if (read == 0) return false;

            totalRead += read;
        }

        return true;
    }

    /// <summary>
    /// Returns an unused, random file name with the given prefix.
    /// </summary>
    /// <returns>An unused, random file name with the given prefix.</returns>
    /// <exception cref="IOException"/>
    private static string GetTempFileName(string prefix)
    {
        string path;
        int i = 0;

        do
        {
            path = $"{prefix}{Path.GetRandomFileName()}";

            if (!File.Exists(path))
            {
                return path;
            }
        }
        while (i++ < 100);

        return ThrowHelper.ThrowIO_CantCreateTempFile<string>(path);
    }

    /// <summary>
    /// Attempts to delete the specified files.
    /// </summary>
    private static void TryDeleteFiles(params IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }
    }
}

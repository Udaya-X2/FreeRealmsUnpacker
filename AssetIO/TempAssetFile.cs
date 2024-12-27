using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace AssetIO;

/// <summary>
/// Provides properties and instance methods for the reading, fixing, and extraction of Free Realms asset .temp files.
/// </summary>
public class TempAssetFile : AssetFile
{
    /// <summary>
    /// Initializes a new instance of <see cref="TempAssetFile"/> from the specified asset .temp file.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public TempAssetFile(string path)
        : this(path, ClientFile.InferTempAssetType(path, requireFullType: false, strict: true))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TempAssetFile"/> from
    /// the specified asset .temp file, with the specified asset type.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public TempAssetFile(string path, AssetType assetType)
        : base(path, assetType, [], findDataFiles: false)
    {
        if (FileType == AssetType.Dat) throw new ArgumentException(SR.Argument_ManifestTempNotSupported);
    }

    /// <summary>
    /// Attempts to scan the asset .temp file for errors, fix the error, and rename it to a regular asset file.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset .temp file was converted; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryConvert([MaybeNullWhen(false)] out AssetFile assetFile)
    {
        if (TryFix(out var fix))
        {
            fix.FixPackTempFile(FullName);

            if (!TryRename(out string? newPath))
            {
                newPath = FullName;
            }

            assetFile = new AssetFile(newPath, Type);
            return true;
        }

        assetFile = null;
        return false;
    }

    /// <summary>
    /// Scans the asset .temp file for errors and creates a fix for the first invalid asset group.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset .temp file contains a fixable error; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public bool TryFix(out ClientFile.FixedAssetGroup fix) => ClientFile.TryFixPackTempFile(FullName, out fix);

    /// <summary>
    /// Attempts to rename the asset .temp file to the name of a regular asset file.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the rename operation was successful; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryRename([MaybeNullWhen(false)] out string newPath)
    {
        const string PackTempFileSuffix = ".pack.temp";

        if (Name.EndsWith(PackTempFileSuffix, StringComparison.OrdinalIgnoreCase))
        {
            string name = Name[..^PackTempFileSuffix.Length];

            if (name.Length >= 3 && int.TryParse(name[^3..], out int digits))
            {
                name = name[..^3];

                do
                {
                    newPath = Path.Combine(Info.DirectoryName!, $"{name}{digits++:D3}.pack");
                }
                while (File.Exists(newPath) && !FilesEqual(FullName, newPath));
            }
            else
            {
                newPath = Path.Combine(Info.DirectoryName!, $"{name}.pack");

                for (int digit = 2; File.Exists(newPath) && !FilesEqual(FullName, newPath); digit++)
                {
                    newPath = Path.Combine(Info.DirectoryName!, $"{name}{digit}.pack");
                }
            }

            Info.MoveTo(newPath, overwrite: true);
            return true;
        }

        newPath = null;
        return false;
    }

    /// <inheritdoc/>
    public override Asset[] Assets => [.. EnumerateAssets()];

    /// <inheritdoc/>
    public override int Count => EnumerateAssets().Count();

    /// <inheritdoc/>
    public override IEnumerable<Asset> EnumerateAssets()
    {
        using IEnumerator<Asset> enumerator = base.EnumerateAssets().GetEnumerator();
        long end = Info.Length;

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
    /// Checks whether the contents of the files are the same.
    /// </summary>
    /// <returns><see langword="true"/> if the files have the same bytes; otherwise, <see langword="false"/>.</returns>
    private static bool FilesEqual(string file1, string file2)
    {
        if (file1 == file2) return true;

        using FileStream stream1 = File.OpenRead(file1);
        using FileStream stream2 = File.OpenRead(file2);

        if (stream1.Length != stream2.Length) return false;

        const int BufferSize = 81920;
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

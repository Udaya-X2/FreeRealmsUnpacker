namespace AssetIO;

/// <summary>
/// Provides random access reading operations on Free Realms asset file(s).
/// </summary>
public abstract class AssetReader : IDisposable
{
    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s) into a byte array.
    /// </summary>
    /// <param name="asset">The asset to read.</param>
    /// <returns>A byte array containing data read from the asset file(s).</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual byte[] Read(Asset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.Size > (uint)Array.MaxLength) throw new IOException(string.Format(SR.IO_AssetTooLong2GB, asset.Name));
        if (asset.Size == 0) return [];

        byte[] bytes = new byte[asset.Size];
        Read(asset, bytes);
        return bytes;
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s) and writes the data in a given buffer.
    /// </summary>
    /// <param name="asset">The asset to read.</param>
    /// <param name="buffer">
    /// When this method returns, contains the specified byte array with the values between 0
    /// and <see cref="Asset.Offset"/> replaced by the the bytes read from the asset file(s).
    /// </param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract void Read(Asset asset, byte[] buffer);

    /// <summary>
    /// Returns a <see cref="Stream"/> that reads the bytes of the specified asset from the asset file(s).
    /// </summary>
    /// <remarks>
    /// The returned <see cref="Stream"/> shares its lifetime with the
    /// <see cref="AssetReader"/>; it does not need to be explicitly disposed.
    /// </remarks>
    /// <param name="asset">The asset to read.</param>
    /// <returns>A <see cref="Stream"/> that reads the bytes of the specified asset from the asset file(s).</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract Stream ReadStream(Asset asset);

    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s) and writes them to another stream.
    /// </summary>
    /// <param name="asset">The asset to copy.</param>
    /// <param name="destination">The stream to which the contents of the asset will be copied.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract void CopyTo(Asset asset, Stream destination);

    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s) and writes them to another asset writer.
    /// </summary>
    /// <param name="asset">The asset to copy.</param>
    /// <param name="writer">The asset writer to which the contents of the asset will be copied.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract void CopyTo(Asset asset, AssetWriter writer);

    /// <inheritdoc cref="ExtractTo(Asset, string, FileConflictOptions, out bool)"/>
    public FileInfo ExtractTo(Asset asset, string dirPath, FileConflictOptions options = FileConflictOptions.Overwrite)
        => ExtractTo(asset, dirPath, options, out _);

    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s)
    /// and writes them to a file in the given directory path.
    /// </summary>
    /// <param name="asset">The asset to extract.</param>
    /// <param name="dirPath">The destination directory path.</param>
    /// <param name="options">Specifies how to handle file conflicts in the destination path.</param>
    /// <param name="fileExtracted">
    /// <see langword="true"/> if the file was extracted; <see langword="false"/> if it was skipped.
    /// </param>
    /// <returns>The extracted file.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public FileInfo ExtractTo(Asset asset, string dirPath, FileConflictOptions options, out bool fileExtracted)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(dirPath);

        fileExtracted = TryGetExtractionPath(asset, dirPath, options, out string filePath);
        FileInfo file = new(filePath);

        if (fileExtracted)
        {
            file.Directory?.Create();
            using FileStream fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
            CopyTo(asset, fs);
        }

        return file;
    }

    /// <summary>
    /// Determines where to extract the specified asset. A return value indicates
    /// whether the path is valid, according to the given file conflict options.
    /// </summary>
    /// <param name="asset">The asset to extract.</param>
    /// <param name="dirPath">The destination directory path.</param>
    /// <param name="options">Specifies how to handle file conflicts in the destination path.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <returns>
    /// <see langword="true"/> if the asset can be extracted to
    /// <paramref name="filePath"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    private bool TryGetExtractionPath(Asset asset, string dirPath, FileConflictOptions options, out string filePath)
    {
        filePath = Path.Combine(dirPath, asset.Name);

        switch (options)
        {
            case FileConflictOptions.Overwrite:
                break;
            case FileConflictOptions.Skip:
                return !File.Exists(filePath);
            case FileConflictOptions.Rename:
                {
                    string? extension = null, pathWithoutExtension = null;

                    for (int digit = 2; File.Exists(filePath); digit++)
                    {
                        if (AssetEquals(asset, filePath)) return false;

                        extension ??= Path.GetExtension(filePath);
                        pathWithoutExtension ??= filePath[..^extension.Length];
                        filePath = $"{pathWithoutExtension} ({digit}){extension}";
                    }
                }
                break;
            case FileConflictOptions.MkDir:
                for (int digit = 2; File.Exists(filePath); digit++)
                {
                    if (AssetEquals(asset, filePath)) return false;

                    filePath = Path.Combine($"{dirPath} ({digit})", asset.Name);
                }
                break;
            case FileConflictOptions.MkSubdir:
                if (File.Exists(filePath))
                {
                    if (AssetEquals(asset, filePath)) return false;

                    string extension = Path.GetExtension(asset.Name);
                    MoveFileDown(filePath, $"1{extension}");
                    filePath = Path.Combine(filePath, $"2{extension}");
                }
                else if (Directory.Exists(filePath))
                {
                    string originalPath = filePath;
                    string extension = Path.GetExtension(asset.Name);
                    filePath = Path.Combine(originalPath, $"1{extension}");

                    for (int digit = 2; File.Exists(filePath); digit++)
                    {
                        if (AssetEquals(asset, filePath)) return false;

                        filePath = Path.Combine(originalPath, $"{digit}{extension}");
                    }
                }
                break;
            case FileConflictOptions.MkTree:
                if (File.Exists(filePath))
                {
                    if (AssetEquals(asset, filePath)) return false;

                    string fileName = Path.GetFileName(asset.Name);
                    MoveFileDown(filePath, Path.Combine("1", fileName));
                    filePath = Path.Combine(filePath, "2", fileName);
                }
                else if (Directory.Exists(filePath))
                {
                    string originalPath = filePath;
                    string fileName = Path.GetFileName(asset.Name);
                    filePath = Path.Combine(originalPath, "1", fileName);

                    for (int digit = 2; File.Exists(filePath); digit++)
                    {
                        if (AssetEquals(asset, filePath)) return false;

                        filePath = Path.Combine(originalPath, $"{digit}", fileName);
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), SR.ArgumentOutOfRange_Enum);
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified asset is equal to the given file.
    /// </summary>
    /// <param name="asset">The asset to read.</param>
    /// <param name="path">The path of the file to compare with the contents of the asset.</param>
    /// <returns>
    /// <see langword="true"/> if the specified asset equals the given file; otherwise, <see langword="false"/>.
    /// </returns>
    private bool AssetEquals(Asset asset, string path)
    {
        FileInfo file = new(path);

        if (asset.Size != file.Length) return false;

        using FileStream fs = file.OpenRead();
        return StreamEquals(asset, fs);
    }

    /// <summary>
    /// Replaces the specified file with a directory and moves
    /// the file inside of the directory with the given name.
    /// </summary>
    /// <param name="path">The path of the specified file, which will be replaced with a directory.</param>
    /// <param name="fileName">The name of the file inside the new directory.</param>
    private static void MoveFileDown(string path, string fileName)
    {
        string tempPath = Path.GetTempFileName();
        File.Move(path, tempPath, overwrite: true);
        FileInfo file = new(Path.Combine(path, fileName));
        file.Directory?.Create();
        File.Move(tempPath, file.FullName);
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s) and computes its CRC-32 value.
    /// </summary>
    /// <param name="asset">The asset to read.</param>
    /// <returns>The CRC-32 checksum value of the specified asset.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract uint GetCrc32(Asset asset);

    /// <summary>
    /// Determines whether the contents of the given asset matches the specified stream.
    /// </summary>
    /// <param name="asset">The asset to read.</param>
    /// <param name="stream">The stream to which the contents of the asset will be compared.</param>
    /// <returns>
    /// <see langword="true"/> if the contents of the asset and
    /// the stream are the same; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract bool StreamEquals(Asset asset, Stream stream);

    /// <summary>
    /// Asynchronously determines whether the contents of the given asset matches the specified stream.
    /// </summary>
    /// <param name="asset">The asset to read.</param>
    /// <param name="stream">The stream to which the contents of the asset will be compared.</param>
    /// <param name="token">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the stream comparison. The result of the task is <see langword="true"/>
    /// if the contents of the asset and the stream are the same; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract Task<bool> StreamEqualsAsync(Asset asset, Stream stream, CancellationToken token = default);

    /// <inheritdoc cref="Dispose()"/>
    protected abstract void Dispose(bool disposing);

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="AssetReader"/> class.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

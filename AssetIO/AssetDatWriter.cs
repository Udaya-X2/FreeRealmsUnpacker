using AssetIO.EndianBinaryIO;
using Force.Crc32;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AssetIO;

/// <summary>
/// Provides sequential asset writing operations on a Free Realms manifest.dat file and accompanying asset .dat files.
/// </summary>
public class AssetDatWriter : AssetWriter
{
    private const int MaxAssetDatSize = 209715200; // The maximum possible size of an asset .dat file.
    private const int ManifestChunkSize = 148; // The size of an asset chunk in a manifest.dat file.
    private const int MaxAssetNameLength = 128; // The maximum asset name length allowed in a manifest.dat file.
    private const int BufferSize = 81920;

    private readonly FileStream _manifestStream;
    private readonly EndianBinaryWriter _manifestWriter;
    private readonly FileMode _mode;
    private readonly byte[] _buffer;
    private readonly byte[] _nameBuffer;

    private bool _disposed;
    private IEnumerator<string> _dataFileEnumerator;
    private FileStream _dataStream;
    private int _dataFileIdx;
    private int _assetNameLength;
    private string? _assetName;
    private long _assetOffset;
    private uint _assetSize;
    private uint _assetCrc32;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetDatWriter"/> class for the specified manifest.dat file.
    /// </summary>
    /// <param name="manifestFile">The manifest.dat file to write.</param>
    /// <param name="append">Whether to append assets instead of overwriting the file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public AssetDatWriter(string manifestFile, bool append = false)
        : this(manifestFile, ClientDirectory.EnumerateDataFilesInfinite(manifestFile), append)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetDatWriter"/> class
    /// for the specified manifest.dat file and asset .dat files.
    /// </summary>
    /// <param name="manifestFile">The manifest.dat file to write.</param>
    /// <param name="dataFiles">The asset .dat files to write to.</param>
    /// <param name="append">Whether to append assets instead of overwriting the file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public AssetDatWriter(string manifestFile, IEnumerable<string> dataFiles, bool append = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(manifestFile);

        try
        {
            // Open the manifest.dat file in little-endian format.
            _mode = append ? FileMode.Append : FileMode.Create;
            _manifestStream = new(manifestFile, _mode, FileAccess.Write, FileShare.Read);
            _manifestWriter = new(_manifestStream, Endian.Little);

            // Create an enumerator to access asset .dat files.
            _dataFileEnumerator = dataFiles.GetEnumerator();
            _dataStream = GetNextDataStream();

            // Skip to the first asset .dat file with free space.
            if (append)
            {
                if (_manifestStream.Length % ManifestChunkSize != 0) ThrowHelper.ThrowIO_BadManifest(manifestFile);

                long size = _dataStream.Length;
                _assetOffset += size;

                while (size >= MaxAssetDatSize)
                {
                    if (size > MaxAssetDatSize) ThrowHelper.ThrowIO_BadAssetDat(size, _dataStream.Name);

                    _dataStream = GetNextDataStream();
                    size = _dataStream.Length;
                    _assetOffset += size;
                }
            }
        }
        catch
        {
            // Clean up disposables if an error occurs.
            _manifestStream?.Dispose();
            _manifestWriter?.Dispose();
            _dataFileEnumerator?.Dispose();
            _dataStream?.Dispose();
            throw;
        }

        // Ensure future asset .dat files are written to from the start of the file.
        _mode = FileMode.Create;
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        _nameBuffer = new byte[MaxAssetNameLength];
    }

    /// <summary>
    /// Gets whether data can be written to the current asset.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_assetName), nameof(Asset))]
    public override bool CanWrite => _assetName != null;

    /// <summary>
    /// Gets the current asset being written to the manifest.dat file and asset .dat file(s).
    /// </summary>
    public override Asset? Asset => CanWrite ? new Asset(_assetName, _assetOffset, _assetSize, _assetCrc32) : null;

    /// <summary>
    /// Adds a new asset with the specified name to the manifest .dat file and asset .dat file(s).
    /// </summary>
    /// <inheritdoc/>
    public override void Add(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (CanWrite) IndexAsset();

        _assetNameLength = GetByteCountUTF8(name);
        _assetName = name;
        _assetSize = 0u;
        _assetCrc32 = 0u;
    }

    /// <inheritdoc/>
    public override void Write(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead) ThrowHelper.ThrowArgument_StreamNotReadable();
        if (!CanWrite) ThrowHelper.ThrowInvalidOperation_NoAssetToWrite();

        int bytesRead;

        // Read blocks of data into the buffer at a time, until all bytes of the asset have been written.
        while ((bytesRead = stream.Read(_buffer, 0, BufferSize)) > 0)
        {
            int spaceLeft = MaxAssetDatSize - (int)_dataStream.Position;

            // If the asset content exceeds the remaining space in the current asset
            // .dat file, write the overflow bytes to the next asset .dat file.
            if (bytesRead > spaceLeft)
            {
                _dataStream.Write(_buffer, 0, spaceLeft);
                _dataStream = GetNextDataStream();
                _dataStream.Write(_buffer, spaceLeft, bytesRead - spaceLeft);
            }
            else
            {
                _dataStream.Write(_buffer, 0, bytesRead);
            }

            // Compute the CRC-32/size of the asset while the data is being written.
            _assetCrc32 = Crc32Algorithm.Append(_assetCrc32, _buffer, 0, bytesRead);
            _assetSize += (uint)bytesRead;
        }
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int index, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if ((uint)count > buffer.Length - index) ThrowHelper.ThrowArgument_InvalidOffLen();
        if (!CanWrite) ThrowHelper.ThrowInvalidOperation_NoAssetToWrite();

        while (count > 0)
        {
            int spaceLeft = MaxAssetDatSize - (int)_dataStream.Position;
            int bytesWritten;

            // If the asset content exceeds the remaining space in the current asset
            // .dat file, write the overflow bytes to the next asset .dat file.
            if (spaceLeft < count)
            {
                bytesWritten = spaceLeft;
                _dataStream.Write(buffer, index, bytesWritten);
                _dataStream = GetNextDataStream();
            }
            else
            {
                bytesWritten = count;
                _dataStream.Write(buffer, index, bytesWritten);
            }

            // Compute the CRC-32/size of the asset while the data is being written.
            _assetCrc32 = Crc32Algorithm.Append(_assetCrc32, buffer, index, bytesWritten);
            _assetSize += (uint)bytesWritten;
            index += bytesWritten;
            count -= bytesWritten;
        }
    }

    /// <summary>
    /// Writes the current asset to the manifest.dat file.
    /// </summary>
    /// <inheritdoc/>
    public override Asset Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!CanWrite) ThrowHelper.ThrowInvalidOperation_NoAssetToFlush();

        Asset asset = new(_assetName, _assetOffset, _assetSize, _assetCrc32);
        IndexAsset();
        return asset;
    }

    /// <summary>
    /// Indexes the current asset in the manifest.dat file.
    /// </summary>
    private void IndexAsset()
    {
        if (_assetSize == 0)
        {
            _assetOffset = 0;
        }

        _manifestWriter.Write(_assetNameLength);
        _manifestStream.Write(_nameBuffer, 0, _assetNameLength);
        _manifestWriter.Write(_assetOffset);
        _manifestWriter.Write(_assetSize);
        _manifestWriter.Write(_assetCrc32);
        _manifestStream.Seek(MaxAssetNameLength - _assetNameLength, SeekOrigin.Current); // Pad with null bytes
        _assetOffset += _assetSize;
        _assetName = null;
    }

    /// <summary>
    /// Encodes the characters from the specified name into a sequence of bytes, stored in <see cref="_nameBuffer"/>.
    /// </summary>
    /// <param name="name">The string containing the characters to encode.</param>
    /// <returns>The number of encoded bytes.</returns>
    /// <exception cref="ArgumentException"/>
    private int GetByteCountUTF8(string name)
    {
        try
        {
            return Encoding.UTF8.GetBytes(name, _nameBuffer);
        }
        catch (ArgumentException ex)
        {
            return ThrowHelper.ThrowArgument_BadAssetName<int>(name, _manifestStream.Name, ex);
        }
    }

    /// <summary>
    /// Closes the current data file stream and opens the next asset .dat file.
    /// </summary>
    /// <returns>A write-only <see cref="FileStream"/> for the next asset .dat file.</returns>
    /// <exception cref="IOException"/>
    private FileStream GetNextDataStream()
    {
        try
        {
            _dataStream?.Dispose();

            // Create a new enumerator if the current one runs out of asset .dat files.
            if (!_dataFileEnumerator.MoveNext())
            {
                _dataFileEnumerator.Dispose();
                _dataFileEnumerator = ClientDirectory.EnumerateDataFilesInfinite(_manifestStream.Name, _dataFileIdx)
                                                     .GetEnumerator();
                _dataFileEnumerator.MoveNext();
            }

            _dataFileIdx++;
            return File.Open(_dataFileEnumerator.Current, _mode, FileAccess.Write, FileShare.Read);
        }
        catch (Exception ex)
        {
            return ThrowHelper.ThrowIO_NoMoreAssetDatFilesWrite<FileStream>(_manifestStream.Name, ex);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            ArrayPool<byte>.Shared.Return(_buffer);

            // Index the current asset, if necessary.
            if (CanWrite)
            {
                IndexAsset();
            }

            // Set length to include null byte padding.
            _manifestStream.SetLength(_manifestStream.Position);
            _manifestStream.Dispose();
            _manifestWriter.Dispose();
            _dataStream.Dispose();

            // Remove any unused asset .dat files.
            using (_dataFileEnumerator)
            {
                while (_dataFileEnumerator.MoveNext() && File.Exists(_dataFileEnumerator.Current))
                {
                    File.Delete(_dataFileEnumerator.Current);
                }
            }
        }

        _disposed = true;
    }
}

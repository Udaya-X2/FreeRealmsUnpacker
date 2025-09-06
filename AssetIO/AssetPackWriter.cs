using AssetIO.EndianBinaryIO;
using Force.Crc32;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AssetIO;

/// <summary>
/// Provides sequential asset writing operations on a Free Realms asset .pack file.
/// </summary>
public class AssetPackWriter : AssetWriter
{
    private const int MaxAssetNameLength = 128; // The maximum asset name length allowed in a manifest.dat file.
    private const int AssetInfoChunkSize = 8192; // The size of an asset info chunk in an asset .pack file.
    private const int AssetInfoHeaderSize = 8; // The size of an asset info chunk header (NextOffset + NumAssets).
    private const int AssetFieldsSize = 16; // The size of an asset's fields (Name.Length + Offset + Size + Crc32).
    private const int BufferSize = 81920;

    private readonly FileStream _packStream;
    private readonly EndianBinaryWriter _packWriter;
    private readonly byte[] _buffer;
    private readonly byte[] _nameBuffer;

    private bool _disposed;
    private uint _chunkOffset;
    private int _chunkSize;
    private uint _numAssets;
    private int _assetNameLength;
    private string? _assetName;
    private uint _assetOffset;
    private uint _assetSize;
    private uint _assetCrc32;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetPackWriter"/> class for the specified asset .pack file.
    /// </summary>
    /// <param name="packFile">The asset .pack file to write.</param>
    /// <param name="append">Whether to append assets instead of overwriting the file.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    public AssetPackWriter(string packFile, bool append = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(packFile);

        // Open the .pack file in big-endian format.
        FileMode mode = append ? FileMode.OpenOrCreate : FileMode.Create;
        FileAccess access = append ? FileAccess.ReadWrite : FileAccess.Write;
        _packStream = new FileStream(packFile, mode, access, FileShare.Read);
        _packWriter = new EndianBinaryWriter(_packStream, Endian.Big);
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        _nameBuffer = new byte[MaxAssetNameLength];
        _chunkSize = AssetInfoHeaderSize;

        try
        {
            // If appending to a non-empty .pack file, skip to the last asset chunk.
            if (append && _packStream.Length != 0)
            {
                using EndianBinaryReader reader = new(_packStream, Endian.Big, Encoding.UTF8, leaveOpen: true);
                uint nextOffset;

                while ((nextOffset = reader.ReadUInt32()) != 0)
                {
                    _chunkOffset = nextOffset;
                    _packStream.Position = nextOffset;
                }

                _numAssets = reader.ReadUInt32();

                // Compute the size of the last asset info chunk.
                for (uint i = 0; i < _numAssets; i++)
                {
                    int length = ClientFile.ValidateNameLength(reader.ReadInt32());
                    int assetIndexSize = length + AssetFieldsSize;
                    _packStream.Seek(assetIndexSize - sizeof(int), SeekOrigin.Current);
                    _chunkSize += assetIndexSize;

                    if (_chunkSize > AssetInfoChunkSize)
                    {
                        throw new IOException(string.Format(SR.IO_BadAssetInfo, _chunkOffset, _packStream.Name));
                    }
                }

                // Expand the last asset info chunk if necessary.
                if (_packStream.Length < _chunkOffset + AssetInfoChunkSize)
                {
                    _packStream.SetLength(_chunkOffset + AssetInfoChunkSize);
                }
            }
            // Otherwise, create an empty asset info chunk at the start of the .pack file.
            else
            {
                _packStream.SetLength(AssetInfoChunkSize);
            }
        }
        catch (InvalidAssetException ex)
        {
            using var _ = this;
            throw new IOException(string.Format(SR.IO_BadAsset, _packStream.Position - ex.Size, _packStream.Name), ex);
        }
        catch (EndOfStreamException ex)
        {
            using var _ = this;
            throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, _packStream.Name), ex);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Gets whether data can be written to the current asset.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_assetName), nameof(Asset))]
    public override bool CanWrite => _assetName != null;

    /// <summary>
    /// Gets the current asset being written to the .pack file.
    /// </summary>
    public override Asset? Asset => CanWrite ? new Asset(_assetName, _assetOffset, _assetSize, _assetCrc32) : null;

    /// <summary>
    /// Adds a new asset with the specified name to the .pack file.
    /// </summary>
    /// <inheritdoc/>
    /// <exception cref="OverflowException"/>
    public override void Add(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (CanWrite) IndexAsset();

        try
        {
            _assetNameLength = GetByteCountUTF8(name);
            _assetOffset = checked((uint)_packStream.Length);
            _assetSize = 0u;
            _assetCrc32 = 0u;

            // If current asset exceeds the space of this asset info chunk, proceed to the next asset chunk.
            if (_chunkSize + _assetNameLength + AssetFieldsSize > AssetInfoChunkSize)
            {
                // Write the offset of the next asset info chunk and the number of assets at the start of the chunk.
                _packStream.Position = _chunkOffset;
                _packWriter.Write(_assetOffset);
                _packWriter.Write(_numAssets);

                // Reset asset info chunk related fields.
                _chunkOffset = _assetOffset;
                _chunkSize = AssetInfoHeaderSize;
                _numAssets = 0;

                // Start the next asset content chunk at the end of the next asset info chunk.
                checked { _assetOffset += AssetInfoChunkSize; }
                _packStream.SetLength(_assetOffset);
            }

            _packStream.Position = _assetOffset;
            _assetName = name;
        }
        catch (OverflowException ex)
        {
            throw new OverflowException(string.Format(SR.Overflow_CantAddAsset, name, _packStream.Name), ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="OverflowException"/>
    public override void Write(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead) throw new ArgumentException(SR.Argument_StreamNotReadable);
        if (!CanWrite) throw new InvalidOperationException(SR.InvalidOperation_NoAssetToWrite);

        try
        {
            int bytesRead;

            // Read blocks of data into the buffer at a time, until all bytes of the asset have been written.
            while ((bytesRead = stream.Read(_buffer, 0, BufferSize)) > 0)
            {
                // Compute the CRC-32/size of the asset while the data is being written.
                _packStream.Write(_buffer, 0, bytesRead);
                _assetCrc32 = Crc32Algorithm.Append(_assetCrc32, _buffer, 0, bytesRead);
                checked { _assetSize += (uint)bytesRead; }
            }
        }
        catch (OverflowException ex)
        {
            throw new OverflowException(string.Format(SR.Overflow_CantAddAsset, _assetName, _packStream.Name), ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="OverflowException"/>
    public override void Write(byte[] buffer, int index, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if ((uint)count > buffer.Length - index) throw new ArgumentException(SR.Argument_InvalidOffLen);
        if (!CanWrite) throw new InvalidOperationException(SR.InvalidOperation_NoAssetToWrite);

        try
        {
            // Compute the CRC-32/size of the asset while the data is being written.
            _packStream.Write(buffer, index, count);
            _assetCrc32 = Crc32Algorithm.Append(_assetCrc32, buffer, index, count);
            checked { _assetSize += (uint)count; }
        }
        catch (OverflowException ex)
        {
            throw new OverflowException(string.Format(SR.Overflow_CantAddAsset, _assetName, _packStream.Name), ex);
        }
    }

    /// <summary>
    /// Writes the current asset to the .pack file.
    /// </summary>
    /// <inheritdoc/>
    public override Asset Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!CanWrite) throw new InvalidOperationException(SR.InvalidOperation_NoAssetToFlush);

        Asset asset = new(_assetName, _assetOffset, _assetSize, _assetCrc32);
        IndexAsset();
        return asset;
    }

    /// <summary>
    /// Indexes the current asset in the asset info chunk.
    /// </summary>
    private void IndexAsset()
    {
        if (_assetSize == 0)
        {
            _assetOffset = 0;
        }

        _packStream.Position = _chunkOffset + _chunkSize;
        _packWriter.Write(_assetNameLength);
        _packStream.Write(_nameBuffer, 0, _assetNameLength);
        _packWriter.Write(_assetOffset);
        _packWriter.Write(_assetSize);
        _packWriter.Write(_assetCrc32);
        _chunkSize += _assetNameLength + AssetFieldsSize;
        _numAssets++;
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
            throw new ArgumentException(string.Format(SR.Argument_InvalidAssetName, name, _packStream.Name), ex);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ArrayPool<byte>.Shared.Return(_buffer);

                // Index the current asset, if necessary.
                if (CanWrite)
                {
                    IndexAsset();
                }
                // Flush the current asset info chunk if necessary, setting the next chunk offset
                // to the start of the .pack file and writing the number of assets in this chunk.
                if (_numAssets > 0)
                {
                    _packStream.Position = _chunkOffset;
                    _packWriter.Write(0u);
                    _packWriter.Write(_numAssets);
                }

                _packStream.Dispose();
                _packWriter.Dispose();
            }

            _disposed = true;
        }
    }
}

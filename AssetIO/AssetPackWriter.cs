using AssetIO.EndianBinaryIO;
using Force.Crc32;
using System.Buffers;

namespace AssetIO;

/// <summary>
/// Provides sequential asset writing operations on a Free Realms asset .pack file.
/// </summary>
public class AssetPackWriter : AssetWriter
{
    private const int MaxAssetNameLength = 128;
    private const int AssetPackInfoChunkSize = 8192;
    private const int AssetPackInfoHeaderSize = 8;
    private const int AssetPackInfoFieldsSize = 16;
    private const int BufferSize = 81920;

    private readonly FileStream _packStream;
    private readonly EndianBinaryWriter _packWriter;
    private readonly byte[] _buffer;

    private bool _disposed;
    private uint _chunkOffset;
    private int _chunkSize;
    private uint _numAssets;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetPackWriter"/> class for the specified asset .pack file.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public AssetPackWriter(string packFile, bool append = false)
    {
        ArgumentNullException.ThrowIfNull(packFile);
        if (append) throw new Exception("Append option not implemented");

        // Open the .pack file in big-endian format.
        _packStream = new FileStream(packFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        _packWriter = new EndianBinaryWriter(_packStream, Endian.Big);

        // Create an empty asset info chunk at the start of the .pack file.
        _packStream.SetLength(AssetPackInfoChunkSize);
        _chunkSize = AssetPackInfoHeaderSize;
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    }

    /// <summary>
    /// Writes an asset with the given name and stream contents to the .pack file.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="PathTooLongException"/>
    public override void Write(Stream stream, string name)
    {
        try
        {
            checked
            {
                int nameLength = ClientFile.ValidateRange(name.Length, 1, MaxAssetNameLength);
                int assetInfoSize = nameLength + AssetPackInfoFieldsSize;
                uint offset = (uint)_packStream.Length;
                uint size = 0u;
                uint crc32 = 0u;
                int bytesRead;

                // If current asset exceeds the space of this asset info chunk, proceed to the next asset chunk.
                if (_chunkSize + assetInfoSize > AssetPackInfoChunkSize)
                {
                    // Write the offset of the next asset info chunk and the number of assets at the start of the chunk.
                    _packStream.Position = _chunkOffset;
                    _packWriter.Write(offset);
                    _packWriter.Write(_numAssets);

                    // Reset asset info chunk related fields.
                    _chunkOffset = offset;
                    _chunkSize = AssetPackInfoHeaderSize;
                    _numAssets = 0;

                    // Start the next asset content chunk at the end of the next asset info chunk.
                    offset += AssetPackInfoChunkSize;
                    _packStream.SetLength(offset);
                }

                _packStream.Position = offset;

                // Read blocks of data into the buffer at a time, until all bytes of the asset have been written.
                while ((bytesRead = stream.Read(_buffer, 0, BufferSize)) != 0)
                {
                    // Compute the CRC-32/size of the asset while the data is being written.
                    _packStream.Write(_buffer, 0, bytesRead);
                    crc32 = Crc32Algorithm.Append(crc32, _buffer, 0, bytesRead);
                    size += (uint)bytesRead;
                }

                if (size == 0)
                {
                    offset = 0;
                }

                // Index the asset in the asset info chunk.
                _packStream.Position = _chunkOffset + _chunkSize;
                _packWriter.Write(name);
                _packWriter.Write(offset);
                _packWriter.Write(size);
                _packWriter.Write(crc32);
                _chunkSize += assetInfoSize;
                _numAssets++;
            }
        }
        catch (OverflowException ex)
        {
            throw new OverflowException(string.Format(SR.Overflow_MaxPackCapacity, name, _packStream.Name), ex);
        }
        catch (ArgumentOutOfRangeException ex) when (ex.Data.Contains("BytesRead"))
        {
            throw new PathTooLongException(string.Format(SR.PathTooLong_CantAddAsset, name, _packStream.Name), ex);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
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
                ArrayPool<byte>.Shared.Return(_buffer);
            }

            _disposed = true;
        }
    }
}

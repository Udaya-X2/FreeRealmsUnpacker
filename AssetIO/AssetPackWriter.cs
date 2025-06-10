using AssetIO.EndianBinaryIO;
using Force.Crc32;
using System.Buffers;
using System.Text;

namespace AssetIO;

/// <summary>
/// Provides sequential asset writing operations on a Free Realms asset .pack file.
/// </summary>
public class AssetPackWriter : AssetWriter
{
    private const int MaxAssetNameLength = 128; // The maximum asset name length allowed in a manifest .dat file.
    private const int AssetPackInfoChunkSize = 8192; // The size of an asset info chunk in an asset .pack file.
    private const int AssetPackInfoHeaderSize = 8; // The size of an asset info chunk header (NextOffset/NumAssets).
    private const int AssetPackInfoFieldsSize = 16; // The size of an asset's fields (Name.Length/Offset/Size/Crc32).
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
    /// <exception cref="EndOfStreamException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="OverflowException"/>
    public AssetPackWriter(string packFile, bool append = false)
    {
        ArgumentNullException.ThrowIfNull(packFile);

        // Open the .pack file in big-endian format.
        FileMode mode = append ? FileMode.OpenOrCreate : FileMode.Create;
        FileAccess access = append ? FileAccess.ReadWrite : FileAccess.Write;
        _packStream = new FileStream(packFile, mode, access, FileShare.Read);
        _packWriter = new EndianBinaryWriter(_packStream, Endian.Big);
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        _chunkSize = AssetPackInfoHeaderSize;

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
                    int nameLength = ClientFile.ValidateRange(reader.ReadInt32(), 1, MaxAssetNameLength);
                    int assetInfoSize = nameLength + AssetPackInfoFieldsSize;
                    _packStream.Seek(assetInfoSize - sizeof(int), SeekOrigin.Current);
                    checked { _chunkSize += assetInfoSize; }
                }
            }
            // Otherwise, create an empty asset info chunk at the start of the .pack file.
            else
            {
                _packStream.SetLength(AssetPackInfoChunkSize);
            }
        }
        catch (ArgumentOutOfRangeException ex) when (ex.Data["BytesRead"] is int bytes)
        {
            Dispose();
            throw new IOException(string.Format(SR.IO_BadAsset, _packStream.Position - bytes, _packStream.Name), ex);
        }
        catch (EndOfStreamException ex)
        {
            Dispose();
            throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, _packStream.Name), ex);
        }
        catch (OverflowException ex)
        {
            Dispose();
            throw new OverflowException(string.Format(SR.Overflow_MaxPackCapacity, _packStream.Name), ex);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Writes an asset with the given name and stream contents to the .pack file.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="PathTooLongException"/>
    public override void Write(string name, Stream stream)
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
            throw new OverflowException(string.Format(SR.Overflow_CantAddAsset, name, _packStream.Name), ex);
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

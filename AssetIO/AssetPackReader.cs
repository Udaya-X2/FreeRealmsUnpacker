using Force.Crc32;
using System.Buffers;

namespace AssetIO;

/// <summary>
/// Provides random access reading operations on a Free Realms asset .pack file.
/// </summary>
public class AssetPackReader : AssetReader
{
    private const int BufferSize = 81920;

    private readonly FileStream _assetStream;
    private readonly byte[] _buffer;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetPackReader"/> class for the specified asset .pack file.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public AssetPackReader(string packFile)
    {
        ArgumentException.ThrowIfNullOrEmpty(packFile);

        _assetStream = File.OpenRead(packFile);
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .pack file and writes the data in a given buffer.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public override void Read(Asset asset, byte[] buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(buffer);
        if (buffer.Length < asset.Size) throw new ArgumentException(SR.Argument_InvalidAssetLen);

        _assetStream.Position = asset.Offset;
        int bytesRead = _assetStream.Read(buffer, 0, (int)asset.Size);

        if (bytesRead != asset.Size)
        {
            throw new IOException(string.Format(SR.IO_AssetEOF, asset.Name, _assetStream.Name));
        }
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .pack file and writes them to another stream.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public override void CopyTo(Asset asset, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException(SR.Argument_StreamNotWritable);

        foreach (int bytesRead in InternalRead(asset))
        {
            destination.Write(_buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .pack file and computes its CRC-32 value.
    /// </summary>
    /// <returns>The CRC-32 checksum value of the specified asset.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public override uint GetCrc32(Asset asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);

        uint crc32 = 0u;

        foreach (int bytesRead in InternalRead(asset))
        {
            crc32 = Crc32Algorithm.Append(crc32, _buffer, 0, bytesRead);
        }

        return crc32;
    }

    /// <inheritdoc/>
    public override bool StreamEquals(Asset asset, Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead) throw new ArgumentException(SR.Argument_StreamNotReadable);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            foreach (int count in InternalRead(asset))
            {
                int bytesRead = stream.Read(buffer, 0, count);

                while (bytesRead != count)
                {
                    int read = stream.Read(buffer, bytesRead, count - bytesRead);

                    if (read == 0) return false;

                    bytesRead += read;
                }

                if (!_buffer.AsSpan(0, count).SequenceEqual(buffer.AsSpan(0, count))) return false;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return true;
    }

    /// <inheritdoc/>
    public override async Task<bool> StreamEqualsAsync(Asset asset, Stream stream, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead) throw new ArgumentException(SR.Argument_StreamNotReadable);
        token.ThrowIfCancellationRequested();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            uint bytes = asset.Size;

            foreach (Task<int> task1 in InternalReadAsync(asset, token))
            {
                token.ThrowIfCancellationRequested();
                int count = BufferSize <= bytes ? BufferSize : (int)bytes;
                Task<int> task2 = stream.ReadAsync(buffer, 0, count, token);
                int[] bytesRead = await Task.WhenAll(task1, task2);
                int bytesRead1 = bytesRead[0];
                int bytesRead2 = bytesRead[1];

                while (bytesRead2 != count)
                {
                    int read = stream.Read(buffer, bytesRead2, count - bytesRead2);

                    if (read == 0) return false;

                    bytesRead2 += read;
                }

                if (!_buffer.AsSpan(0, count).SequenceEqual(buffer.AsSpan(0, count))) return false;

                bytes -= (uint)count;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return true;
    }

    /// <summary>
    /// Reads blocks of bytes of the specified asset from the .dat file(s) into the internal buffer.
    /// </summary>
    /// <returns>An enumerable sequence of the number of bytes read into the buffer at each read.</returns>
    private IEnumerable<int> InternalRead(Asset asset)
    {
        _assetStream.Position = asset.Offset;
        uint bytes = asset.Size;

        // Read blocks of data into the buffer at a time, until all bytes of the asset have been read.
        while (bytes > 0u)
        {
            int count = BufferSize <= bytes ? BufferSize : (int)bytes;

            if (_assetStream.Read(_buffer, 0, count) != count)
            {
                throw new IOException(string.Format(SR.IO_AssetEOF, asset.Name, _assetStream.Name));
            }

            yield return count;
            bytes -= (uint)count;
        }
    }

    /// <summary>
    /// Asynchronously reads blocks of bytes of the specified asset from the .dat file(s) into the internal buffer.
    /// </summary>
    /// <returns>
    /// An enumerable sequence of the number of bytes read into the buffer at each asynchronous read.
    /// </returns>
    private IEnumerable<Task<int>> InternalReadAsync(Asset asset, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        _assetStream.Position = asset.Offset;
        uint bytes = asset.Size;

        // Read blocks of data into the buffer at a time, until all bytes of the asset have been read.
        while (bytes > 0u)
        {
            token.ThrowIfCancellationRequested();
            int count = BufferSize <= bytes ? BufferSize : (int)bytes;
            yield return Task.Run(async () =>
            {
                int bytesRead = await _assetStream.ReadAsync(_buffer.AsMemory(0, count), token);

                if (bytesRead != count)
                {
                    throw new IOException(string.Format(SR.IO_AssetEOF, asset.Name, _assetStream.Name));
                }

                return bytesRead;
            });
            bytes -= (uint)count;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _assetStream.Dispose();
                ArrayPool<byte>.Shared.Return(_buffer);
            }

            _disposed = true;
        }
    }
}

using Force.Crc32;
using System.Buffers;

namespace AssetIO;

/// <summary>
/// Provides random access reading operations on a Free Realms asset .pack file.
/// </summary>
public class AssetPackReader : AssetReader
{
    private const int BufferSize = 81920;

    private readonly FileStream _stream;
    private readonly byte[] _buffer;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetPackReader"/> class for the specified asset .pack file.
    /// </summary>
    /// <param name="packFile">The asset .pack file to read.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public AssetPackReader(string packFile)
    {
        ArgumentException.ThrowIfNullOrEmpty(packFile);

        _stream = File.OpenRead(packFile);
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .pack file and writes the data in a given buffer.
    /// </summary>
    /// <inheritdoc/>
    public override void Read(Asset asset, byte[] buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(buffer);
        if ((uint)buffer.Length < asset.Size) ThrowHelper.ThrowArgument_InvalidAssetLen();

        _stream.Position = asset.Offset;
        int bytesRead = _stream.Read(buffer, 0, (int)asset.Size);

        if (bytesRead != asset.Size) ThrowHelper.ThrowIO_AssetEOF(asset.Name, _stream.Name);
    }

    /// <summary>
    /// Returns a <see cref="Stream"/> that reads the bytes of the specified asset from the .pack file.
    /// </summary>
    /// <returns>A <see cref="Stream"/> that reads the bytes of the specified asset from the .pack file.</returns>
    /// <inheritdoc/>
    public override Stream ReadStream(Asset asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);

        return new AssetStream(_stream, asset);
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .pack file and writes them to another stream.
    /// </summary>
    /// <inheritdoc/>
    public override void CopyTo(Asset asset, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) ThrowHelper.ThrowArgument_StreamNotWritable();

        foreach (int bytesRead in InternalRead(asset))
        {
            destination.Write(_buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .pack file and writes them to another asset writer.
    /// </summary>
    /// <inheritdoc/>
    public override void CopyTo(Asset asset, AssetWriter writer)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(writer);

        writer.Add(asset.Name);

        foreach (int bytesRead in InternalRead(asset))
        {
            writer.Write(_buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .pack file and computes its CRC-32 value.
    /// </summary>
    /// <returns>The CRC-32 checksum value of the specified asset.</returns>
    /// <inheritdoc/>
    public override uint GetCrc32(Asset asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);

        uint crc32 = 0;

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
        if (!stream.CanRead) ThrowHelper.ThrowArgument_StreamNotReadable();
        if (asset.Size != stream.Length) return false;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        try
        {
            foreach (int count in InternalRead(asset))
            {
                int totalRead = 0;

                do
                {
                    int read = stream.Read(buffer, totalRead, count - totalRead);

                    if (read == 0) return false;

                    totalRead += read;
                }
                while (totalRead != count) ;

                if (!_buffer.AsSpan(0, count).SequenceEqual(buffer.AsSpan(0, count))) return false;
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
        _stream.Position = asset.Offset;
        uint bytes = asset.Size;

        // Read blocks of data into the buffer at a time, until all bytes of the asset have been read.
        while (bytes > 0)
        {
            int count = BufferSize <= bytes ? BufferSize : (int)bytes;

            if (_stream.Read(_buffer, 0, count) != count) ThrowHelper.ThrowIO_AssetEOF(asset.Name, _stream.Name);

            yield return count;
            bytes -= (uint)count;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _stream.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Provides a read-only view of the bytes from the specified asset in the .pack file.
    /// </summary>
    /// <param name="stream">The asset .pack file's stream.</param>
    /// <param name="asset">The asset to read.</param>
    private sealed class AssetStream(FileStream stream, Asset asset) : Stream
    {
        private long _position = asset.Offset;

        /// <inheritdoc/>
        public override bool CanRead => stream.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => stream.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => asset.Size;

        /// <inheritdoc/>
        public override long Position
        {
            get => _position - asset.Offset;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                _position = asset.Offset + value;
            }
        }

        /// <inheritdoc/>
        public override void Flush() => stream.Flush();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            long bytesLeft = asset.Offset + asset.Size - _position;

            if (bytesLeft <= 0) return 0;
            if (count > bytesLeft) count = (int)bytesLeft;

            stream.Position = _position;

            if (stream.Read(buffer, offset, count) != count) ThrowHelper.ThrowIO_AssetEOF(asset.Name, stream.Name);

            _position += count;
            return count;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => origin switch
        {
            SeekOrigin.Begin => Position = offset,
            SeekOrigin.Current => Position += offset,
            SeekOrigin.End => Position = Length + offset,
            _ => ThrowHelper.ThrowArgument_InvalidSeekOrigin<long>(nameof(origin))
        };

        /// <inheritdoc/>
        public override void SetLength(long value)
            => ThrowHelper.ThrowNotSupported_UnwritableStream();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
            => ThrowHelper.ThrowNotSupported_UnwritableStream();
    }
}

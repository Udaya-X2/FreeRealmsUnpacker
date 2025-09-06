using Force.Crc32;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace AssetIO;

/// <summary>
/// Provides random access reading operations on Free Realms asset .dat files.
/// </summary>
public class AssetDatReader : AssetReader
{
    private const int MaxAssetDatSize = 209715200; // The maximum possible size of an asset .dat file.
    private const int BufferSize = 81920;

    private readonly FileStream[] _streams;
    private readonly byte[] _buffer;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetDatReader"/> class for the specified asset .dat files.
    /// </summary>
    /// <param name="dataFiles">The asset .dat files to read.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    public AssetDatReader(IEnumerable<string> dataFiles)
    {
        ArgumentNullException.ThrowIfNull(dataFiles);

        _streams = OpenReadDataFiles(dataFiles);
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .dat file(s) and writes the data in a given buffer.
    /// </summary>
    /// <inheritdoc/>
    public override void Read(Asset asset, byte[] buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(buffer);
        if ((uint)buffer.Length < asset.Size) throw new ArgumentException(SR.Argument_InvalidAssetLen);

        // Determine which .dat file to read and where to start reading from based on the offset.
        long file = asset.Offset / MaxAssetDatSize;
        long address = asset.Offset % MaxAssetDatSize;
        FileStream stream = GetStream(file, asset);
        stream.Position = address;
        int bytesRead = stream.Read(buffer, 0, (int)asset.Size);

        // If the asset spans multiple files, read the next .dat file(s) to obtain the rest of the asset.
        while (bytesRead != asset.Size)
        {
            stream = GetStream(++file, asset);
            stream.Position = 0;
            bytesRead += stream.Read(buffer, bytesRead, (int)asset.Size - bytesRead);
        }
    }

    /// <summary>
    /// Returns a <see cref="Stream"/> that reads the bytes of the specified asset from the .dat file(s).
    /// </summary>
    /// <returns>A <see cref="Stream"/> that reads the bytes of the specified asset from the .dat file(s).</returns>
    /// <inheritdoc/>
    public override Stream ReadStream(Asset asset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);

        return new AssetStream(_streams, asset);
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .dat file(s) and writes them to another stream.
    /// </summary>
    /// <inheritdoc/>
    public override void CopyTo(Asset asset, Stream destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException(SR.Argument_StreamNotWritable);

        foreach (int bytesRead in InternalRead(asset))
        {
            destination.Write(_buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .dat file(s) and writes them to another asset writer.
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
    /// Reads the bytes of the specified asset from the .dat file(s) and computes its CRC-32 value.
    /// </summary>
    /// <returns>The CRC-32 checksum value of the specified asset.</returns>
    /// <inheritdoc/>
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
        // Determine which .dat file to read and where to start reading from based on the offset.
        long file = asset.Offset / MaxAssetDatSize;
        long address = asset.Offset % MaxAssetDatSize;
        FileStream stream = GetStream(file, asset);
        stream.Position = address;
        uint bytes = asset.Size;

        // Read blocks of data into the buffer at a time, until all bytes of the asset have been read.
        while (bytes > 0u)
        {
            int count = BufferSize <= bytes ? BufferSize : (int)bytes;
            int bytesRead = stream.Read(_buffer, 0, count);

            // If the asset spans multiple files, read the next .dat file(s) to obtain the rest of the asset.
            while (bytesRead != count)
            {
                stream = GetStream(++file, asset);
                stream.Position = 0;
                bytesRead += stream.Read(_buffer, bytesRead, count - bytesRead);
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
        // Determine which .dat file to read and where to start reading from based on the offset.
        token.ThrowIfCancellationRequested();
        long file = asset.Offset / MaxAssetDatSize;
        long address = asset.Offset % MaxAssetDatSize;
        FileStream stream = GetStream(file, asset);
        stream.Position = address;
        uint bytes = asset.Size;

        // Read blocks of data into the buffer at a time, until all bytes of the asset have been read.
        while (bytes > 0u)
        {
            token.ThrowIfCancellationRequested();
            int count = BufferSize <= bytes ? BufferSize : (int)bytes;
            yield return Task.Run(async () =>
            {
                int bytesRead = await stream.ReadAsync(_buffer.AsMemory(0, count), token);

                // If the asset spans multiple files, read the next .dat file(s) to obtain the rest of the asset.
                while (bytesRead != count)
                {
                    stream = GetStream(++file, asset);
                    stream.Position = 0;
                    bytesRead += stream.Read(_buffer, bytesRead, count - bytesRead);
                }

                return bytesRead;
            });
            bytes -= (uint)count;
        }
    }

    /// <inheritdoc cref="GetStream(FileStream[], long, Asset)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FileStream GetStream(long file, Asset asset) => GetStream(_streams, file, asset);

    /// <summary>
    /// Returns the <see cref="FileStream"/> at the specified index.
    /// </summary>
    /// <returns>The <see cref="FileStream"/> at the specified index.</returns>
    /// <exception cref="IOException"/>
    private static FileStream GetStream(FileStream[] streams, long file, Asset asset)
    {
        try
        {
            return streams[file];
        }
        catch
        {
            throw new IOException(string.Format(SR.IO_NoMoreAssetDatFilesRead, asset.Name));
        }
    }

    /// <summary>
    /// Opens the specified asset .dat files for reading.
    /// </summary>
    /// <returns>An array of read-only <see cref="FileStream"/> objects on the specified files.</returns>
    private static FileStream[] OpenReadDataFiles(IEnumerable<string> files)
    {
        List<FileStream> fileStreams = [];
        FileStream? fileStream = null;

        try
        {
            foreach (string file in files)
            {
                // Break early if the previous asset .dat file doesn't meet the size requirement.
                if (fileStream?.Length < MaxAssetDatSize)
                {
                    break;
                }

                fileStream = File.OpenRead(file);
                fileStreams.Add(fileStream);
            }
        }
        catch
        {
            // If some files were opened before the error occurred, dispose them before throwing.
            fileStreams.ForEach(x => x.Dispose());
            throw;
        }

        return [.. fileStreams];
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                Array.ForEach(_streams, x => x.Dispose());
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Provides a read-only view of the bytes from the specified asset in the .dat file(s).
    /// </summary>
    /// <param name="streams">The asset .dat file streams.</param>
    /// <param name="asset">The asset to read.</param>
    private sealed class AssetStream(FileStream[] streams, Asset asset) : Stream
    {
        private long _position = asset.Offset;

        /// <inheritdoc/>
        public override bool CanRead => Array.TrueForAll(streams, x => x.CanRead);

        /// <inheritdoc/>
        public override bool CanSeek => Array.TrueForAll(streams, x => x.CanSeek);

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
        public override void Flush() => Array.ForEach(streams, x => x.Flush());

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            long bytesLeft = asset.Offset + asset.Size - _position;

            if (bytesLeft <= 0) return 0;
            if (count > bytesLeft) count = (int)bytesLeft;

            // Determine which .dat file to read and where to start reading from based on the current position.
            long file = _position / MaxAssetDatSize;
            long address = _position % MaxAssetDatSize;
            FileStream stream = GetStream(streams, file, asset);
            stream.Position = address;
            int bytesRead = stream.Read(buffer, offset, count);

            // If the asset spans multiple files, read the next .dat file(s) to obtain the rest of the asset.
            while (bytesRead != count)
            {
                stream = GetStream(streams, ++file, asset);
                stream.Position = 0;
                bytesRead += stream.Read(buffer, offset + bytesRead, count - bytesRead);
            }

            _position += count;
            return count;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => origin switch
        {
            SeekOrigin.Begin => Position = offset,
            SeekOrigin.Current => Position += offset,
            SeekOrigin.End => Position = Length + offset,
            _ => throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin))
        };

        /// <inheritdoc/>
        public override void SetLength(long value)
            => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException(SR.NotSupported_UnwritableStream);
    }
}

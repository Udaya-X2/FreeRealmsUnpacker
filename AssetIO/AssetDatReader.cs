using Force.Crc32;
using System.Buffers;

namespace AssetIO;

/// <summary>
/// Provides random access reading operations on Free Realms asset .dat files.
/// </summary>
public class AssetDatReader : AssetReader
{
    private const int MaxAssetDatSize = 209715200;
    private const int BufferSize = 81920;

    private readonly FileStream[] _assetStreams;
    private readonly byte[] _buffer;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetDatReader"/> class for the specified asset .dat files.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public AssetDatReader(IEnumerable<string> assetDatPaths)
    {
        if (assetDatPaths == null) throw new ArgumentNullException(nameof(assetDatPaths));

        _assetStreams = OpenReadFiles(assetDatPaths);
        _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .dat file(s) and writes the data in a given buffer.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public override void Read(Asset asset, byte[] buffer)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AssetDatReader));
        if (asset == null) throw new ArgumentNullException(nameof(asset));
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (buffer.Length < asset.Size) throw new ArgumentException(SR.Argument_InvalidAssetLen);

        // Determine which .dat file to read and where to start reading from based on the offset.
        long file = asset.Offset / MaxAssetDatSize;
        long address = asset.Offset % MaxAssetDatSize;
        FileStream assetStream = GetAssetStream(file, asset);
        assetStream.Position = address;
        int bytesRead = assetStream.Read(buffer, 0, (int)asset.Size);

        // If the asset spans multiple files, read the next .dat file(s) to obtain the rest of the asset.
        while (bytesRead != asset.Size)
        {
            assetStream = GetAssetStream(++file, asset);
            assetStream.Position = 0;
            bytesRead += assetStream.Read(buffer, bytesRead, (int)asset.Size - bytesRead);
        }
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .dat file(s) and writes them to another stream.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public override void CopyTo(Asset asset, Stream destination)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AssetDatReader));
        if (asset == null) throw new ArgumentNullException(nameof(asset));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite) throw new ArgumentException(SR.Argument_StreamNotWritable);

        foreach (int bytesRead in InternalRead(asset))
        {
            destination.Write(_buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// Reads the bytes of the specified asset from the .dat file(s) and computes its CRC-32 value.
    /// </summary>
    /// <returns>The CRC-32 checksum value of the specified asset.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public override uint GetCrc32(Asset asset)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AssetDatReader));
        if (asset == null) throw new ArgumentNullException(nameof(asset));

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
        if (_disposed) throw new ObjectDisposedException(nameof(AssetPackReader));
        if (asset == null) throw new ArgumentNullException(nameof(asset));
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new ArgumentException(SR.Argument_StreamNotReadable);

        // Determine which .dat file to read and where to start reading from based on the offset.
        long file = asset.Offset / MaxAssetDatSize;
        long address = asset.Offset % MaxAssetDatSize;
        FileStream assetStream = GetAssetStream(file, asset);
        assetStream.Position = address;
        uint bytes = asset.Size;
        byte[] buffer2 = ArrayPool<byte>.Shared.Rent(BufferSize);

        while (bytes > 0u)
        {
            int count = BufferSize <= bytes ? BufferSize : (int)bytes;
            Task<int> task1 = assetStream.ReadAsync(_buffer, 0, count);
            Task<int> task2 = stream.ReadAsync(buffer2, 0, count);
            int[] bytesRead = Task.WhenAll(task1, task2).Result;
            int bytesRead1 = bytesRead[0];
            int bytesRead2 = bytesRead[1];

            // If the asset spans multiple files, read the next .dat file(s) to obtain the rest of the asset.
            while (bytesRead1 != count)
            {
                assetStream = GetAssetStream(++file, asset);
                assetStream.Position = 0;
                bytesRead1 += assetStream.Read(_buffer, bytesRead1, count - bytesRead1);
            }
            while (bytesRead2 != count)
            {
                int read = stream.Read(buffer2, bytesRead2, count - bytesRead2);

                if (read == 0) return false;

                bytesRead2 += read;
            }

            if (!_buffer.AsSpan(0, count).SequenceEqual(buffer2.AsSpan(0, count))) return false;

            bytes -= (uint)count;
        }

        ArrayPool<byte>.Shared.Return(buffer2);
        return true;
    }

    /// <summary>
    /// Incrementally reads blocks of bytes of the specified asset from the .dat file(s) into the internal buffer.
    /// </summary>
    /// <returns>An enumerable sequence of the number of bytes read into the buffer at each read.</returns>
    private IEnumerable<int> InternalRead(Asset asset)
    {
        // Determine which .dat file to read and where to start reading from based on the offset.
        long file = asset.Offset / MaxAssetDatSize;
        long address = asset.Offset % MaxAssetDatSize;
        FileStream assetStream = GetAssetStream(file, asset);
        assetStream.Position = address;
        uint bytes = asset.Size;

        // Read blocks of data into the buffer at a time, until all bytes of the asset have been read.
        while (bytes > 0u)
        {
            int count = BufferSize <= bytes ? BufferSize : (int)bytes;
            int bytesRead = assetStream.Read(_buffer, 0, count);

            // If the asset spans multiple files, read the next .dat file(s) to obtain the rest of the asset.
            while (bytesRead != count)
            {
                assetStream = GetAssetStream(++file, asset);
                assetStream.Position = 0;
                bytesRead += assetStream.Read(_buffer, bytesRead, count - bytesRead);
            }

            yield return count;
            bytes -= (uint)count;
        }
    }

    /// <summary>
    /// Returns the <see cref="FileStream"/> at the specified index.
    /// </summary>
    /// <returns>The <see cref="FileStream"/> at the specified index.</returns>
    /// <exception cref="IOException"/>
    private FileStream GetAssetStream(long file, Asset asset)
    {
        try
        {
            return _assetStreams[file];
        }
        catch
        {
            throw new IOException(string.Format(SR.IO_NoMoreAssetDatFiles, asset.Name));
        }
    }

    /// <summary>
    /// Opens the specified files for reading.
    /// </summary>
    /// <returns>An array of read-only <see cref="FileStream"/> objects on the specified files.</returns>
    private static FileStream[] OpenReadFiles(IEnumerable<string> files)
    {
        List<FileStream> fileStreams = new();

        foreach (string file in files)
        {
            try
            {
                fileStreams.Add(File.OpenRead(file));
            }
            catch
            {
                // If some files were opened before the error occurred, dispose them before throwing.
                fileStreams.ForEach(x => x.Dispose());
                throw;
            }
        }

        return fileStreams.ToArray();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Array.ForEach(_assetStreams, x => x.Dispose());
                ArrayPool<byte>.Shared.Return(_buffer);
            }

            _disposed = true;
        }
    }
}

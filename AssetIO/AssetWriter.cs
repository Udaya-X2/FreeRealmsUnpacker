using System.Diagnostics.CodeAnalysis;

namespace AssetIO;

/// <summary>
/// Provides sequential asset writing operations on Free Realms asset file(s).
/// </summary>
public abstract class AssetWriter : IDisposable
{
    /// <summary>
    /// Gets whether data can be written to the current asset.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Asset))]
    public abstract bool CanWrite { get; }

    /// <summary>
    /// Gets the current asset being written to the asset file(s).
    /// </summary>
    public abstract Asset? Asset { get; }

    /// <summary>
    /// Adds a new asset with the specified name to the asset file(s).
    /// </summary>
    /// <param name="name">The name of the asset.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract void Add(string name);

    /// <summary>
    /// Writes the contents of the specified stream to the current asset.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract void Write(Stream stream);

    /// <summary>
    /// Writes the current asset to the asset file(s).
    /// </summary>
    /// <returns>The asset written.</returns>
    /// <exception cref="InvalidOperationException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract Asset Flush();

    /// <summary>
    /// Writes a region of a byte array to the current asset.
    /// </summary>
    /// <param name="buffer">A byte array containing the data to write.</param>
    /// <param name="index">
    /// The index of the first byte to read from <paramref name="buffer"/> and to write to the asset.
    /// </param>
    /// <param name="count">
    /// The number of bytes to read from <paramref name="buffer"/> and to write to the asset.
    /// </param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="InvalidOperationException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual void Write(byte[] buffer, int index, int count) => Write(new MemoryStream(buffer, index, count));

    /// <summary>
    /// Writes a byte array to the current asset.
    /// </summary>
    /// <param name="buffer">A byte array containing the data to write.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);

    /// <summary>
    /// Writes an asset with the name and contents of the given file to the asset file(s).
    /// </summary>
    /// <param name="file">The path of the file to write as an asset.</param>
    /// <returns>The asset written.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual Asset Write(string file) => Write(new FileInfo(file));

    /// <summary>
    /// Writes an asset with the name and contents of the given file to the asset file(s).
    /// </summary>
    /// <param name="file">The file to write as an asset.</param>
    /// <returns>The asset written.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual Asset Write(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        
        using FileStream stream = new(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 0);
        return Write(file.Name, stream);
    }

    /// <summary>
    /// Writes an asset with the given name and stream contents to the asset file(s).
    /// </summary>
    /// <param name="name">The name of the asset.</param>
    /// <param name="stream">The stream from which the contents of the asset will be copied.</param>
    /// <returns>The asset written.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual Asset Write(string name, Stream stream)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead) ThrowHelper.ThrowArgument_StreamNotReadable();

        Add(name);
        Write(stream);
        return Flush();
    }

    /// <summary>
    /// Writes an asset with the given name and bytes to the asset file(s).
    /// </summary>
    /// <param name="name">The name of the asset.</param>
    /// <param name="buffer">A byte array containing the data to write.</param>
    /// <returns>The asset written.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual Asset Write(string name, byte[] buffer) => Write(name, buffer, 0, buffer.Length);

    /// <summary>
    /// Writes an asset with the given name and bytes to the asset file(s).
    /// </summary>
    /// <param name="name">The name of the asset.</param>
    /// <param name="buffer">A byte array containing the data to write.</param>
    /// <param name="index">
    /// The index of the first byte to read from <paramref name="buffer"/> and to write to the asset.
    /// </param>
    /// <param name="count">
    /// The number of bytes to read from <paramref name="buffer"/> and to write to the asset.
    /// </param>
    /// <returns>The asset written.</returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual Asset Write(string name, byte[] buffer, int index, int count)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if ((uint)count > buffer.Length - index) ThrowHelper.ThrowArgument_InvalidOffLen();

        Add(name);
        Write(buffer, index, count);
        return Flush();
    }

    /// <inheritdoc cref="Dispose()"/>
    protected abstract void Dispose(bool disposing);

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="AssetWriter"/> class.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

namespace AssetIO;

/// <summary>
/// Provides sequential asset writing operations on Free Realms asset file(s).
/// </summary>
public abstract class AssetWriter : IDisposable
{
    /// <summary>
    /// Writes an asset with the name and contents of the given file to the asset file(s).
    /// </summary>
    /// <param name="file">The path of the file to write as an asset.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual void Write(string file) => Write(new FileInfo(file));

    /// <summary>
    /// Writes an asset with the name and contents of the given file to the asset file(s).
    /// </summary>
    /// <param name="file">The file to write as an asset.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual void Write(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using FileStream stream = file.OpenRead();
        Write(file.Name, stream);
    }

    /// <summary>
    /// Writes an asset with the given name and bytes to the asset file(s).
    /// </summary>
    /// <param name="name">The name of the asset.</param>
    /// <param name="buffer">The buffer containing the data to write to the asset.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual void Write(string name, byte[] buffer)
        => Write(name, buffer, 0, buffer.Length);

    /// <summary>
    /// Writes an asset with the given name and bytes to the asset file(s).
    /// </summary>
    /// <param name="name">The name of the asset.</param>
    /// <param name="buffer">The buffer containing the data to write to the asset.</param>
    /// <param name="index">
    /// The zero-based byte offset in <paramref name="buffer"/> from which to begin copying bytes.
    /// </param>
    /// <param name="count">The maximum number of bytes to write.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual void Write(string name, byte[] buffer, int index, int count)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(buffer);

        using MemoryStream ms = new(buffer, index, count);
        Write(name, ms);
    }

    /// <summary>
    /// Writes an asset with the given name and stream contents to the asset file(s).
    /// </summary>
    /// <param name="name">The name of the asset.</param>
    /// <param name="stream">The stream from which the contents of the asset will be copied.</param>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract void Write(string name, Stream stream);

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

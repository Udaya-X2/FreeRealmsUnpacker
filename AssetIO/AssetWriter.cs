namespace AssetIO;

/// <summary>
/// Provides sequential asset writing operations on Free Realms asset file(s).
/// </summary>
public abstract class AssetWriter : IDisposable
{
    /// <summary>
    /// Writes an asset with the name and contents of the given file to the asset file(s).
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual void Write(string file) => Write(new FileInfo(file));
 
    /// <summary>
    /// Writes an asset with the name and contents of the given file to the asset file(s).
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public virtual void Write(FileInfo file)
    {
        using FileStream stream = file.OpenRead();
        Write(file.Name, stream);
    }

    /// <summary>
    /// Writes an asset with the given name and stream contents to the asset file(s).
    /// </summary>
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

namespace AssetIO;

/// <summary>
/// Provides random access reading operations on Free Realms asset file(s).
/// </summary>
public abstract class AssetReader : IDisposable
{
    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s) and writes the data in a given buffer.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract void Read(Asset asset, byte[] buffer);

    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s) and writes them to another stream.
    /// </summary>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract void CopyTo(Asset asset, Stream destination);

    /// <summary>
    /// Reads the bytes of the specified asset from the asset file(s) and computes its CRC-32 value.
    /// </summary>
    /// <returns>The CRC-32 checksum value of the specified asset.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="ObjectDisposedException"/>
    public abstract uint GetCrc32(Asset asset);

    /// <inheritdoc cref="Dispose()"/>
    protected abstract void Dispose(bool disposing);

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="AssetReader"/> class.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

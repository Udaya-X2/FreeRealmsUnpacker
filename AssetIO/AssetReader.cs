namespace AssetIO
{
    /// <summary>
    /// Provides random access reading operations on Free Realms asset files.
    /// </summary>
    public abstract class AssetReader : IDisposable
    {
        /// <summary>
        /// Creates a new instance of <see cref="AssetReader"/> from the specified asset file.
        /// </summary>
        /// <returns>An <see cref="AssetReader"/> on the specified asset file.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static AssetReader Create(string assetFile) => Path.GetExtension(assetFile) switch
        {
            ".dat" => new AssetDatReader(assetFile),
            ".pack" => new AssetPackReader(assetFile),
            _ => throw new ArgumentException(string.Format(SR.Argument_UnknownAssetExt, assetFile))
        };

        /// <summary>
        /// Reads the contents of the specified asset from the asset file(s) and writes the data in a given buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        public abstract void Read(Asset asset, byte[] buffer);

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
}

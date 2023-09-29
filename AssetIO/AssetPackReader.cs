namespace AssetIO
{
    /// <summary>
    /// Provides random access reading operations on Free Realms asset .pack files.
    /// </summary>
    public class AssetPackReader : AssetReader
    {
        private readonly FileStream _assetStream;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPackReader"/> class for the specified asset .pack file.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public AssetPackReader(string assetPackPath)
        {
            if (assetPackPath == null) throw new ArgumentNullException(nameof(assetPackPath));

            _assetStream = File.OpenRead(assetPackPath);
        }

        /// <summary>
        /// Reads the contents of the specified asset from the .pack file and writes the data in a given buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="EndOfStreamException"/>
        public override void Read(Asset asset, byte[] buffer)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AssetPackReader));
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            _assetStream.Position = asset.Offset;
            int bytesRead = _assetStream.Read(buffer, 0, asset.Size);

            if (bytesRead != asset.Size)
            {
                throw new EndOfStreamException(string.Format(SR.EndOfStream_Asset, asset.Name, _assetStream.Name));
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
                }

                _disposed = true;
            }
        }
    }
}

namespace AssetIO
{
    /// <summary>
    /// Provides random access reading operations on asset packs in a Free Realms client directory.
    /// </summary>
    public class AssetPackReader : IDisposable
    {
        private const string ManifestFileSuffix = "manifest.dat";
        private const int MaxAssetPackSize = 209715200;

        private readonly FileStream[] _assetStreams;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPackReader"/> class
        /// for all asset packs of the specified type in the client directory.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public AssetPackReader(string manifestPath)
        {
            if (manifestPath == null) throw new ArgumentNullException(nameof(manifestPath));

            manifestPath = Path.GetFullPath(manifestPath);
            string manifestDirectory = Path.GetDirectoryName(manifestPath)!;
            string assetPackPattern = GetAssetPackPattern(manifestPath);
            _assetStreams = Directory.EnumerateFiles(manifestDirectory, assetPackPattern)
                                     .Select(File.OpenRead)
                                     .ToArray();
        }

        /// <summary>
        /// Determines which asset pack(s) to open based on the name of the manifest file.
        /// </summary>
        /// <returns>The search pattern to find the asset pack(s) corresponding to the manifest.</returns>
        private static string GetAssetPackPattern(string manifestPath)
        {
            if (!manifestPath.EndsWith(ManifestFileSuffix))
            {
                throw new ArgumentException(string.Format(SR.Argument_BadManifestPath, manifestPath));
            }

            ReadOnlySpan<char> manifestFile = Path.GetFileName(manifestPath.AsSpan());
            ReadOnlySpan<char> manifestFilePrefix = manifestFile[..^ManifestFileSuffix.Length];
            return $"{manifestFilePrefix}???.dat";
        }

        /// <summary>
        /// Reads the contents of the specified asset from the asset pack(s) and writes the data in a given buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        public void Read(Asset asset, byte[] buffer)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AssetPackReader));
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            try
            {
                // Determine which asset pack to read and where to start reading from based on the offset.
                long file = asset.Offset / MaxAssetPackSize;
                long address = asset.Offset % MaxAssetPackSize;
                FileStream assetStream = _assetStreams[file];
                assetStream.Position = address;
                int bytesRead = assetStream.Read(buffer, 0, asset.Size);

                // If the asset spans multiple asset packs, read the next pack(s) to obtain the rest of the asset.
                while (bytesRead != asset.Size)
                {
                    assetStream = _assetStreams[++file];
                    assetStream.Position = 0;
                    bytesRead += assetStream.Read(buffer, bytesRead, asset.Size - bytesRead);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new IOException(string.Format(SR.IO_NoMoreAssetPacks, asset.Name), ex);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                Array.ForEach(_assetStreams, x => x.Dispose());
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }
    }
}

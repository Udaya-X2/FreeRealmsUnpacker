namespace AssetIO
{
    /// <summary>
    /// Provides random access reading operations on Free Realms asset .dat files.
    /// </summary>
    public class AssetDatReader : AssetReader
    {
        private const string ManifestFileSuffix = "manifest.dat";
        private const int MaxAssetDatSize = 209715200;

        private readonly FileStream[] _assetStreams;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetDatReader"/> class for
        /// all asset .dat files corresponding to the specified manifest.dat file.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public AssetDatReader(string manifestPath)
        {
            if (manifestPath == null) throw new ArgumentNullException(nameof(manifestPath));

            manifestPath = Path.GetFullPath(manifestPath);
            string manifestDirectory = Path.GetDirectoryName(manifestPath)!;
            string assetDatPattern = GetAssetDatPattern(manifestPath);
            _assetStreams = OpenReadFiles(Directory.EnumerateFiles(manifestDirectory, assetDatPattern));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetDatReader"/> class for the specified asset .dat files.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public AssetDatReader(IEnumerable<string> assetDatPaths)
        {
            if (assetDatPaths == null) throw new ArgumentNullException(nameof(assetDatPaths));

            _assetStreams = OpenReadFiles(assetDatPaths);
        }

        /// <summary>
        /// Reads the contents of the specified asset from the .dat file(s) and writes the data in a given buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        public override void Read(Asset asset, byte[] buffer)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AssetDatReader));
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            try
            {
                // Determine which .dat file to read and where to start reading from based on the offset.
                long file = asset.Offset / MaxAssetDatSize;
                long address = asset.Offset % MaxAssetDatSize;
                FileStream assetStream = _assetStreams[file];
                assetStream.Position = address;
                int bytesRead = assetStream.Read(buffer, 0, asset.Size);

                // If the asset spans multiple files, read the next .dat file(s) to obtain the rest of the asset.
                while (bytesRead != asset.Size)
                {
                    assetStream = _assetStreams[++file];
                    assetStream.Position = 0;
                    bytesRead += assetStream.Read(buffer, bytesRead, asset.Size - bytesRead);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new IOException(string.Format(SR.IO_NoMoreAssetDatFiles, asset.Name), ex);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Array.ForEach(_assetStreams, x => x.Dispose());
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Determines which asset pack(s) to open based on the name of the manifest file.
        /// </summary>
        /// <returns>The search pattern to find the asset pack(s) corresponding to the manifest.</returns>
        private static string GetAssetDatPattern(string manifestPath)
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
    }
}

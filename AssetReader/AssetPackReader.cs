using System.ComponentModel;

namespace AssetReader
{
    /// <summary>
    /// Provides random access reading operations on asset packs in a Free Realms client directory.
    /// </summary>
    public class AssetPackReader : IDisposable
    {
        private const int MaxAssetPackSize = 209715200;

        private readonly FileStream[] _assetStreams;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPackReader"/> class
        /// for all asset packs of the specified type in the client directory.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public AssetPackReader(string clientPath, AssetType assetType)
        {
            if (clientPath == null) throw new ArgumentNullException(nameof(clientPath));

            string assetPackPattern = GetAssetPackPattern(assetType);
            _assetStreams = Directory.EnumerateFiles(clientPath, assetPackPattern, SearchOption.AllDirectories)
                                     .Select(File.OpenRead)
                                     .ToArray();
        }

        /// <summary>
        /// Determines which asset pack(s) to open based on the specified asset type.
        /// </summary>
        /// <returns>The search pattern to find the asset pack(s) of the specified asset type.</returns>
        /// <exception cref="InvalidEnumArgumentException"/>
        private static string GetAssetPackPattern(AssetType assetType) => assetType switch
        {
            AssetType.Game => "Assets_???.dat",
            AssetType.Tcg => "assetpack000_000.dat",
            AssetType.Resource => "AssetsTcg_000.dat",
            _ => throw new InvalidEnumArgumentException(nameof(assetType), (int)assetType, assetType.GetType())
        };

        /// <summary>
        /// Reads the contents of the specified asset from the asset pack(s) and writes the data in a given buffer.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IndexOutOfRangeException"/>
        /// <exception cref="ObjectDisposedException"/>
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
                throw new IndexOutOfRangeException(string.Format(SR.IndexOutOfRange_NoMoreAssets, asset.Name), ex);
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

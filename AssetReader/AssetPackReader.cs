namespace AssetReader
{
    /// <summary>
    /// Provides random access reading operations on asset packs in a Free Realms client directory.
    /// </summary>
    public class AssetPackReader : IDisposable
    {
        private const int MaxAssetPackSize = 209715200;

        private readonly FileStream[] _assetStreams;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPackReader"/>, which acts as a combination
        /// file stream reader on all asset packs of the specified type in the client directory.
        /// </summary>
        public AssetPackReader(string clientPath, AssetType assetType)
        {
            string assetPackPattern = GetAssetPackPattern(assetType);
            _assetStreams = Directory.EnumerateFiles(clientPath, assetPackPattern, SearchOption.AllDirectories)
                                     .Select(File.OpenRead)
                                     .ToArray();
        }

        /// <summary>
        /// Determines which asset pack(s) to open based on the specified asset type.
        /// </summary>
        /// <returns>The search pattern to find the asset pack(s) of the specified asset type.</returns>
        /// <exception cref="ArgumentException"></exception>
        private static string GetAssetPackPattern(AssetType assetType) => assetType switch
        {
            AssetType.Game => "Assets_???.dat",
            AssetType.Tcg => "assetpack000_000.dat",
            AssetType.Resource => "AssetsTcg_000.dat",
            _ => throw new ArgumentException("Invalid enum value for asset type", nameof(assetType))
        };

        /// <summary>
        /// Reads the contents of the specified asset from the asset pack(s) and writes the data in a given buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void Read(Asset asset, byte[] buffer)
        {
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
            catch (IndexOutOfRangeException)
            {
                throw new IndexOutOfRangeException($"Ran out of asset packs while reading '{asset.Name}'");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Array.ForEach(_assetStreams, x => x.Dispose());
            GC.SuppressFinalize(this);
        }
    }
}

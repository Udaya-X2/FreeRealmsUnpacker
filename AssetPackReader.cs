namespace FreeRealmsUnpacker
{
    public class AssetPackReader : IDisposable
    {
        private const int MaxAssetPackSize = 209715200;

        private readonly FileStream[] assetStreams;

        /// <summary>
        /// Initializes a new instance of the AssetPackReader class to open all
        /// asset packs of the specified type in the client directory for reading.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public AssetPackReader(string clientPath, AssetType assetType)
        {
            string assetPackPattern = GetAssetPackPattern(assetType);
            assetStreams = Directory.EnumerateFiles(clientPath, assetPackPattern, SearchOption.AllDirectories)
                                    .Select(f => File.OpenRead(f))
                                    .ToArray();

            if (assetStreams.Length == 0)
            {
                throw new ArgumentException($"{clientPath} does not contain any {assetPackPattern} file(s).");
            }
        }

        /// <summary>
        /// Determines which asset pack(s) to open based on the specified asset type.
        /// </summary>
        /// <returns>The search pattern to find the asset pack(s) of the specified asset type.</returns>
        /// <exception cref="ArgumentException"></exception>
        private static string GetAssetPackPattern(AssetType assetType) => assetType switch
        {
            AssetType.Game => "Assets_???.dat",
            AssetType.TCG => "assetpack000_000.dat",
            AssetType.Resource => "AssetsTcg_000.dat",
            _ => throw new ArgumentException("Invalid enum value for extraction type", nameof(assetType)),
        };

        /// <summary>
        /// Reads the contents of the specified asset from the asset pack(s) and writes the data in a given buffer.
        /// </summary>
        public void Read(Asset asset, byte[] buffer)
        {
            // Determine which asset pack to read and where to start reading from the asset address.
            long file = asset.Address / MaxAssetPackSize;
            long address = asset.Address % MaxAssetPackSize;
            FileStream assetStream = assetStreams[file];
            assetStream.Position = address;
            int bytesRead = assetStream.Read(buffer, 0, asset.Size);

            // If the asset spans two asset packs, begin reading the next pack to obtain the rest of the asset.
            if (bytesRead != asset.Size)
            {
                assetStream = assetStreams[file + 1];
                assetStream.Position = 0;
                assetStream.Read(buffer, 0, asset.Size);
            }
        }

        public void Dispose()
        {
            Array.ForEach(assetStreams ?? Array.Empty<FileStream>(), x => x.Dispose());
            GC.SuppressFinalize(this);
        }
    }
}

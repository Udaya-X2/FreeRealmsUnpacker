using System.Text;

namespace FreeRealmsUnpacker
{
    public class ManifestReader
    {
        private const int ManifestChunkSize = 148;

        /// <summary>
        /// Scans the client directory's manifest file for information on each asset of the specified type.
        /// </summary>
        /// <returns>
        /// An array consisting of the assets in the client manifest file,
        /// or an empty array if the manifest file could not be found.
        /// </returns>
        public static Asset[] GetClientAssets(string clientPath, AssetType assetType)
        {
            string manifestPattern = GetManifestPattern(assetType);
            string[] manifestPaths = Directory.GetFiles(clientPath, manifestPattern, SearchOption.AllDirectories);

            if (manifestPaths.Length == 0) return Array.Empty<Asset>();

            using FileStream stream = File.OpenRead(manifestPaths[0]);
            using BinaryReader reader = new(stream);
            Asset[] clientAssets = new Asset[stream.Length / ManifestChunkSize];

            // Scan each chunk of the manifest file for information regarding each asset.
            for (int i = 0; i < clientAssets.Length; i++)
            {
                int length = reader.ReadInt32();
                string name = Encoding.ASCII.GetString(reader.ReadBytes(length));
                long address = reader.ReadInt64();
                int size = reader.ReadInt32();
                uint crc32 = reader.ReadUInt32();
                clientAssets[i] = new Asset(name, address, size, crc32);
                reader.BaseStream.Seek(128 - length, SeekOrigin.Current);
            }

            return clientAssets;
        }

        /// <summary>
        /// Determines which manifest file to use based on the specified asset type.
        /// </summary>
        /// <returns>The search pattern to find the manifest file of the specified asset type.</returns>
        /// <exception cref="ArgumentException"></exception>
        private static string GetManifestPattern(AssetType assetType) => assetType switch
        {
            AssetType.Game => "Assets_manifest.dat",
            AssetType.TCG => "assetpack000_manifest.dat",
            AssetType.Resource => "AssetsTcg_manifest.dat",
            _ => throw new ArgumentException("Invalid enum value for extraction type", nameof(assetType))
        };
    }
}

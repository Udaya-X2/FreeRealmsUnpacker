namespace FreeRealmsUnpacker
{
    public class AssetPackReader : IDisposable
    {
        private const int MaxAssetPackSize = 209715200;

        private readonly FileStream[] assetStreams;

        public AssetPackReader(string clientPath)
        {
            assetStreams = Directory.EnumerateFiles(clientPath, "Assets_???.dat", SearchOption.AllDirectories)
                                    .Select(f => File.OpenRead(f))
                                    .ToArray();

            if (assetStreams.Length == 0)
            {
                throw new ArgumentException("Directory does not contain an Assets_###.dat file.");
            }
        }

        public void Read(Asset asset, byte[] buffer)
        {
            long file = asset.Address / MaxAssetPackSize;
            long address = asset.Address % MaxAssetPackSize;
            FileStream assetStream = assetStreams[file];
            assetStream.Position = address;
            int bytesRead = assetStream.Read(buffer, 0, asset.Size);

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

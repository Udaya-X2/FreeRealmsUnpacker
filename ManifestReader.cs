using System.Text;

namespace FreeRealmsUnpacker
{
    public class ManifestReader
    {
        private const string ManifestFile = "Assets_manifest.dat";
        private const long ManifestChunkSize = 148;

        public static Asset[] GetClientAssets(string clientPath)
        {
            string path = Directory.EnumerateFiles(clientPath, ManifestFile, SearchOption.AllDirectories).First();
            using FileStream stream = File.OpenRead(path);
            using BinaryReader reader = new(stream);
            Asset[] clientAssets = new Asset[stream.Length / ManifestChunkSize];

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
    }
}

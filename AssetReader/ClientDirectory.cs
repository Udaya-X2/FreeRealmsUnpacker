using System.ComponentModel;

namespace AssetReader
{
    /// <summary>
    /// Provides static methods for obtaining asset information in a Free Realms client directory.
    /// </summary>
    public static class ClientDirectory
    {
        private const int ManifestChunkSize = 148;
        private const int MaxAssetNameLength = 128;

        /// <summary>
        /// Scans the assets from the specified file path.
        /// </summary>
        /// <returns>An array consisting of the assets in the specified file.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static Asset[] GetAssets(string assetPath) => Path.GetExtension(assetPath) switch
        {
            ".dat" => GetManifestAssets(assetPath),
            ".pack" => GetPackAssets(assetPath),
            _ => throw new ArgumentException(string.Format(SR.Argument_UnknownAsset, assetPath), nameof(assetPath))
        };

        /// <summary>
        /// Returns the number of assets in the specified file path.
        /// </summary>
        /// <returns>The number of assets in the specified file.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static int GetAssetCount(string assetPath) => Path.GetExtension(assetPath) switch
        {
            ".dat" => GetManifestAssetCount(assetPath),
            ".pack" => GetPackAssetCount(assetPath),
            _ => throw new ArgumentException(string.Format(SR.Argument_UnknownAsset, assetPath), nameof(assetPath))
        };

        /// <summary>
        /// Scans the client directory's manifest.dat file for information on each asset of the specified type.
        /// </summary>
        /// <returns>
        /// An array consisting of the assets in the manifest.dat
        /// file, or an empty array if the file could not be found.
        /// </returns>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public static Asset[] GetManifestAssets(string clientPath, AssetType assetType)
        {
            if (clientPath == null) throw new ArgumentNullException(nameof(clientPath));

            string manifestPattern = GetManifestPattern(assetType);
            string[] manifestPaths = Directory.GetFiles(clientPath, manifestPattern, SearchOption.AllDirectories);

            return manifestPaths.Length switch
            {
                0 => Array.Empty<Asset>(),
                1 => GetManifestAssets(manifestPaths[0]),
                _ => throw new ArgumentException(SR.Argument_TooManyManifestFiles)
            };
        }

        /// <summary>
        /// Scans the manifest.dat file for information on each asset.
        /// </summary>
        /// <returns>An array consisting of the assets in the manifest.dat file.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        public static Asset[] GetManifestAssets(string manifestPath)
        {
            if (manifestPath == null) throw new ArgumentNullException(nameof(manifestPath));

            // Read the manifest.dat file in little-endian format.
            using FileStream stream = File.OpenRead(manifestPath);
            using EndianBinaryReader reader = new(stream, Endian.Little);
            Asset[] clientAssets = new Asset[stream.Length / ManifestChunkSize];

            if (stream.Length % ManifestChunkSize != 0)
            {
                throw new IOException(string.Format(SR.IO_BadManifest, stream.Name));
            }

            // manifest.dat files are divided into 148-byte chunks of data with the following format:
            // 
            // Positions    Sample Value    Description
            // 1-4          14              Length of the asset's name, in bytes.
            // 5-X          mines_pink.def  Name of the asset.
            // X-X+8        826             Offset of the asset in the asset pack(s).
            // X+8-X+12     25              Size of the asset, in bytes.
            // X+12-X+16    3577151519      CRC-32 checksum of the asset.
            // X+16-148     0               Null bytes for the rest of the chunk.
            // 
            // Scan each manifest chunk for information regarding each asset.
            try
            {
                for (int i = 0; i < clientAssets.Length; i++)
                {
                    int length = ValidateRange(reader.ReadInt32(), minValue: 1, maxValue: MaxAssetNameLength);
                    string name = reader.ReadString(length);
                    long offset = ValidateRange(reader.ReadInt64(), minValue: 0);
                    int size = ValidateRange(reader.ReadInt32(), minValue: 0);
                    uint crc32 = reader.ReadUInt32();
                    clientAssets[i] = new Asset(name, offset, size, crc32);
                    stream.Seek(MaxAssetNameLength - length, SeekOrigin.Current);
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new IOException(string.Format(SR.IO_BadAsset, stream.Position, stream.Name), ex);
            }

            return clientAssets;
        }

        /// <summary>
        /// Returns the number of assets in the specified manifest.dat file.
        /// </summary>
        /// <returns>The number of assets in the specified manifest.dat file.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        public static int GetManifestAssetCount(string manifestPath)
        {
            if (manifestPath == null) throw new ArgumentNullException(nameof(manifestPath));

            FileInfo manifestFile = new(manifestPath);

            return manifestFile.Length % ManifestChunkSize == 0
                ? (int)(manifestFile.Length / ManifestChunkSize)
                : throw new IOException(string.Format(SR.IO_BadManifest, manifestPath));
        }

        /// <summary>
        /// Scans the .pack file for information on each asset.
        /// </summary>
        /// <param name="packPath"></param>
        /// <returns>An array consisting of the assets in the .pack file.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="EndOfStreamException"/>
        /// <exception cref="IOException"/>
        public static Asset[] GetPackAssets(string packPath)
        {
            if (packPath == null) throw new ArgumentNullException(nameof(packPath));

            // Read the .pack file in big-endian format.
            using FileStream stream = File.OpenRead(packPath);
            using EndianBinaryReader reader = new(stream, Endian.Big);
            List<Asset> assets = new();
            uint nextOffset = 0;

            // .pack files store assets via two types of data:
            // 
            // * Asset information chunks, which specify what
            //   assets are in the file and where they are.
            // 
            // * Asset content chunks, which store the bytes of each
            //   asset in sequential, contiguous regions of memory.
            // 
            // Asset information chunks begin with a preamble in the following format:
            // 
            // Positions    Sample Value    Description
            // 1-4          2170048         Offset of the next asset info chunk.
            // 5-8          197             Number of assets in the current chunk.
            // 
            // Following the preamble, each asset in the chunk has the following format:
            // 
            // Positions    Sample Value    Description
            // 1-4          13              Length of the asset's name, in bytes.
            // 5-X          ComicBook.lst   Name of the asset.
            // X-X+4        1889108         Offset of the asset in the .pack file.
            // X+4-X+8      338             Size of the asset, in bytes.
            // X+8-X+12     1085288468      CRC-32 checksum of the asset.
            // 
            // Scan each asset info chunk of the .pack file for information regarding each asset.
            try
            {
                do
                {
                    stream.Position = nextOffset;
                    nextOffset = reader.ReadUInt32();
                    int numAssets = ValidateRange(reader.ReadInt32(), minValue: 1);

                    for (int i = 0; i < numAssets; i++)
                    {
                        int length = ValidateRange(reader.ReadInt32(), minValue: 1, maxValue: MaxAssetNameLength);
                        string name = reader.ReadString(length);
                        uint offset = reader.ReadUInt32();
                        int size = ValidateRange(reader.ReadInt32(), minValue: 0);
                        uint crc32 = reader.ReadUInt32();
                        assets.Add(new Asset(name, offset, size, crc32));
                    }
                } while (nextOffset != 0);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new IOException(string.Format(SR.IO_BadAsset, stream.Position, stream.Name), ex);
            }
            catch (EndOfStreamException ex)
            {
                throw new EndOfStreamException(string.Format(SR.EndOfStream_File, stream.Name), ex);
            }

            return assets.ToArray();
        }

        /// <summary>
        /// Returns the number of assets in the specified .pack file.
        /// </summary>
        /// <returns>The number of assets in the specified .pack file.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        public static int GetPackAssetCount(string packPath)
        {
            if (packPath == null) throw new ArgumentNullException(nameof(packPath));

            // Read the .pack file in big-endian format.
            using FileStream stream = new(packPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8);
            using EndianBinaryReader reader = new(stream, Endian.Big);
            uint nextOffset = 0;
            int numAssets = 0;

            // Read the number of assets in each asset info chunk.
            try
            {
                do
                {
                    stream.Position = nextOffset;
                    nextOffset = reader.ReadUInt32();
                    numAssets += ValidateRange(reader.ReadInt32(), minValue: 1);
                } while (nextOffset != 0);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new IOException(string.Format(SR.IO_BadAsset, stream.Position, stream.Name), ex);
            }
            catch (EndOfStreamException ex)
            {
                throw new EndOfStreamException(string.Format(SR.EndOfStream_File, stream.Name), ex);
            }

            return numAssets;
        }

        /// <summary>
        /// Determines which manifest.dat file to use based on the specified asset type.
        /// </summary>
        /// <returns>The search pattern to find the manifest.dat file of the specified asset type.</returns>
        /// <exception cref="InvalidEnumArgumentException"/>
        private static string GetManifestPattern(AssetType assetType) => assetType switch
        {
            AssetType.Game => "Assets_manifest.dat",
            AssetType.Tcg => "assetpack000_manifest.dat",
            AssetType.Resource => "AssetsTcg_manifest.dat",
            _ => throw new InvalidEnumArgumentException(nameof(assetType), (int)assetType, assetType.GetType())
        };

        /// <summary>
        /// Throws an exception if the specified 32-bit integer is outside the given range.
        /// </summary>
        /// <returns>The specified 32-bit integer.</returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        private static int ValidateRange(int n, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            return minValue <= n && n <= maxValue ? n : throw new ArgumentOutOfRangeException(nameof(n));
        }

        /// <summary>
        /// Throws an exception if the specified 64-bit integer is outside the given range.
        /// </summary>
        /// <returns>The specified 64-bit integer.</returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        private static long ValidateRange(long n, long minValue = long.MinValue, long maxValue = long.MaxValue)
        {
            return minValue <= n && n <= maxValue ? n : throw new ArgumentOutOfRangeException(nameof(n));
        }
    }
}

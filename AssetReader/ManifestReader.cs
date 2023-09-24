using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AssetReader
{
    /// <summary>
    /// Provides static methods for obtaining asset information in a Free Realms client directory.
    /// </summary>
    public static class ManifestReader
    {
        private const int ManifestChunkSize = 148;
        private const int MaxAssetNameLength = 128;

        /// <summary>
        /// Scans the client directory's manifest file for information on each asset of the specified type.
        /// </summary>
        /// <returns>
        /// An array consisting of the assets in the client manifest file,
        /// or an empty array if the manifest file could not be found.
        /// </returns>
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
        /// Scans the manifest file for information on each asset.
        /// </summary>
        /// <returns>An array consisting of the assets in the manifest file.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FormatException"/>
        public static Asset[] GetManifestAssets(string manifestPath)
        {
            if (manifestPath == null) throw new ArgumentNullException(nameof(manifestPath));

            // Read the manifest file in little-endian format.
            using FileStream stream = File.OpenRead(manifestPath);
            using EndianBinaryReader reader = new(stream, Endian.Little);
            Asset[] clientAssets = new Asset[stream.Length / ManifestChunkSize];

            if (stream.Length % ManifestChunkSize != 0)
            {
                throw new FormatException(string.Format(SR.Format_BadManifest, stream.Name));
            }

            // Manifest files are divided into 148-byte chunks of data with the following format:
            // 
            // Positions    Sample Value    Description
            // 1-4          14              Length of the asset's name, in bytes.
            // 5-X          mines_pink.def  Name of the asset.
            // X-X+8        826             Offset of the asset in the asset pack(s).
            // X+8-X+12     25              Size of the asset, in bytes.
            // X+12-X+16    3577151519      CRC-32 checksum of the asset.
            // X+16-148     0               Null bytes for the rest of the chunk.
            // 
            // Scan each chunk of the manifest file for information regarding each asset.
            for (int i = 0; i < clientAssets.Length; i++)
            {
                int length = reader.ReadInt32();

                if (length is <= 0 or > MaxAssetNameLength)
                {
                    throw CreateFormatException(SR.Format_BadAssetNameLength, length, stream);
                }

                string name = reader.ReadString(length);
                long offset = reader.ReadInt64();

                if (offset < 0) throw CreateFormatException(SR.Format_BadAssetOffset, offset, stream);

                int size = reader.ReadInt32();

                if (size < 0) throw CreateFormatException(SR.Format_BadAssetSize, size, stream);

                uint crc32 = reader.ReadUInt32();
                clientAssets[i] = new Asset(name, offset, size, crc32);
                stream.Seek(MaxAssetNameLength - length, SeekOrigin.Current);
            }

            return clientAssets;
        }

        /// <summary>
        /// Determines which manifest file to use based on the specified asset type.
        /// </summary>
        /// <returns>The search pattern to find the manifest file of the specified asset type.</returns>
        /// <exception cref="InvalidEnumArgumentException"/>
        private static string GetManifestPattern(AssetType assetType) => assetType switch
        {
            AssetType.Game => "Assets_manifest.dat",
            AssetType.Tcg => "assetpack000_manifest.dat",
            AssetType.Resource => "AssetsTcg_manifest.dat",
            _ => throw new InvalidEnumArgumentException(nameof(assetType), (int)assetType, assetType.GetType())
        };

        /// <summary>
        /// Creates an instance of <see cref="FormatException"/> with a message specifying
        /// the invalid data, position, and filename that caused the exception.
        /// </summary>
        /// <returns>The <see cref="FormatException"/> created from the specified parameters.</returns>
        private static FormatException CreateFormatException<T>(string message, T value, FileStream stream)
        {
            Debug.Assert(typeof(T).IsPrimitive, "FormatException value must be a primitive type.");
            string position = $"0x{stream.Position - Marshal.SizeOf(value):X}";
            return new FormatException(string.Format(message, value, position, stream.Name));
        }
    }
}

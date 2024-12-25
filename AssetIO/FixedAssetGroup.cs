using AssetIO.EndianBinaryIO;

namespace AssetIO;

public static partial class ClientFile
{
    /// <summary>
    /// Represents a fix for the first invalid asset group in a .pack.temp file.
    /// </summary>
    /// <param name="Offset">The byte offset of the group in the file.</param>
    /// <param name="NumAssets">The number of assets in the group, or zero if left unchanged.</param>
    public readonly record struct FixedAssetGroup(uint Offset, uint NumAssets)
    {
        /// <summary>
        /// Fixes the specified .pack.temp file.
        /// </summary>
        public void FixPackTempFile(string packTempFile)
        {
            // Write to the .pack.temp file in big-endian format.
            using FileStream stream = File.OpenWrite(packTempFile);
            using EndianBinaryWriter writer = new(stream, Endian.Big);
            stream.Position = Offset;

            // Set the next offset to zero.
            writer.Write(0);

            // If specified, set the new number of assets.
            if (NumAssets != 0)
            {
                writer.Write(NumAssets);
            }
        }
    }
}

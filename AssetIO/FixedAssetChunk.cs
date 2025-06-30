using AssetIO.EndianBinaryIO;

namespace AssetIO;

public static partial class ClientFile
{
    /// <summary>
    /// Represents a fix for the first invalid asset info chunk in a .pack.temp file.
    /// </summary>
    /// <param name="Offset">The byte offset of the chunk in the file.</param>
    /// <param name="NumAssets">The number of assets in the chunk, or zero if left unchanged.</param>
    public readonly record struct FixedAssetChunk(uint Offset, uint NumAssets)
    {
        /// <summary>
        /// Fixes the specified asset .pack.temp file.
        /// </summary>
        /// <param name="packTempFile">The asset .pack.temp file.</param>
        /// <exception cref="IOException"/>
        /// <exception cref="FileNotFoundException"/>
        public void FixPackTempFile(string packTempFile)
        {
            // Write to the .pack.temp file in big-endian format.
            using FileStream stream = new(packTempFile, FileMode.Open, FileAccess.Write, FileShare.Read, bufferSize: 8);
            using EndianBinaryWriter writer = new(stream, Endian.Big);
            stream.Position = Offset;

            // Set the next offset to zero.
            writer.Write(0u);

            // If specified, set the new number of assets.
            if (NumAssets != 0)
            {
                writer.Write(NumAssets);
            }
        }
    }
}
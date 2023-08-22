namespace FreeRealmsUnpacker
{
    /// <summary>
    /// Represents a Free Realms asset in terms of its file properties.
    /// </summary>
    public class Asset
    {
        public readonly string Name;
        public readonly long Address;
        public readonly int Size;
        public readonly uint CRC32;

        /// <summary>
        /// Initializes a new instance of the <see cref="Asset"/> class,
        /// which stores file information on a Free Realms asset.
        /// </summary>
        public Asset(string name, long address, int size, uint crc32)
        {
            Name = name;
            Address = address;
            Size = size;
            CRC32 = crc32;
        }
    }
}

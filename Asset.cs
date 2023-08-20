namespace FreeRealmsUnpacker
{
    public class Asset
    {
        public readonly string Name;
        public readonly long Address;
        public readonly int Size;
        public readonly uint CRC32;

        /// <summary>
        /// Represents a Free Realms asset in terms of its file properties.
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

namespace AssetIO.EndianBinaryIO
{
    /// <summary>
    /// Indicates the order in which bytes are arranged into larger numerical values in memory.
    /// </summary>
    internal enum Endian
    {
        /// <summary>
        /// The least significant byte is stored in the smallest address.
        /// </summary>
        Little,
        /// <summary>
        /// The most significant byte is stored in the smallest address.
        /// </summary>
        Big
    }
}

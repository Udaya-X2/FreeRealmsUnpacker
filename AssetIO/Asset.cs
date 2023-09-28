namespace AssetIO
{
    /// <summary>
    /// Represents a Free Realms asset in terms of its file properties.
    /// </summary>
    /// <param name="Name">The name of the asset.</param>
    /// <param name="Offset">The byte offset of the asset in the asset packs.</param>
    /// <param name="Size">The size of the asset, in bytes.</param>
    /// <param name="Crc32">The CRC-32 checksum of the asset.</param>
    public record Asset(string Name, long Offset, int Size, uint Crc32);
}

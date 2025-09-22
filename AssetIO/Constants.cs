namespace AssetIO;

/// <summary>
/// Defines library-wide constants related to assets, asset files, and other I/O.
/// </summary>
internal static class Constants
{
    /// <summary>The size of an asset chunk in a manifest.dat file.</summary>
    internal const int ManifestChunkSize = 148;
    /// <summary>The maximum asset name length allowed in a manifest.dat file.</summary>
    internal const int MaxAssetNameLength = 128;
    /// <summary>The maximum possible size of an asset .dat file.</summary>
    internal const int MaxAssetDatSize = 209715200;
    /// <summary>The size of an asset info chunk in an asset .pack file.</summary>
    internal const int PackChunkSize = 8192;
    /// <summary>The size of an asset info chunk header in an asset .pack file (NextOffset + NumAssets).</summary>
    internal const int PackHeaderSize = 8;
    /// <summary>The size of an asset's fields in an asset .pack file (Name.Length + Offset + Size + Crc32).</summary>
    internal const int PackAssetSize = 16;
    /// <summary>The file suffix of an asset .pack file.</summary>
    internal const string PackFileSuffix = ".pack";
    /// <summary>The file suffix of a manifest.dat file.</summary>
    internal const string ManifestFileSuffix = "_manifest.dat";
    /// <summary>The file suffix of an asset .pack.temp file.</summary>
    internal const string PackTempFileSuffix = ".pack.temp";
    /// <summary>The file suffix of an asset .dat file.</summary>
    internal const string DatFileSuffix = ".dat";
    /// <summary>The default buffer size.</summary>
    internal const int BufferSize = 131072;
}

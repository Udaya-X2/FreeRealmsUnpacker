using System.Text.RegularExpressions;

namespace AssetIO;

/// <summary>
/// Defines library-wide regular expressions related to asset file names.
/// </summary>
internal static partial class RegexConstants
{
    [GeneratedRegex(@"^Assets(W?_\d{3}\.pack(\.temp)?|_manifest\.dat)$", Options, "en-US")]
    internal static partial Regex GameAssetRegex { get; }
    [GeneratedRegex(@"^Assets_\d{3}\.dat$", Options, "en-US")]
    internal static partial Regex GameDataRegex { get; }
    [GeneratedRegex(@"^assets_ps3w?_\d{3}\.pack(\.temp)?$", Options, "en-US")]
    internal static partial Regex PS3AssetRegex { get; }
    [GeneratedRegex(@"^AssetsTcg(W?_\d{3}\.pack(\.temp)?|_manifest\.dat)$", Options, "en-US")]
    internal static partial Regex ResourceAssetRegex { get; }
    [GeneratedRegex(@"^AssetsTcg_\d{3}\.dat$", Options, "en-US")]
    internal static partial Regex ResourceDataRegex { get; }
    [GeneratedRegex(@"^assetpack000(W?_\d{3}\.pack(\.temp)?|_manifest\.dat)$", Options, "en-US")]
    internal static partial Regex TcgAssetRegex { get; }
    [GeneratedRegex(@"^assetpack000_\d{3}\.dat$", Options, "en-US")]
    internal static partial Regex TcgDataRegex { get; }

    /// <summary>The options to use for regular expressions defined in this file.</summary>
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
}

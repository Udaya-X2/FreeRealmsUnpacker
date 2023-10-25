using System.ComponentModel;
using System.Text.RegularExpressions;

namespace AssetIO;

/// <summary>
/// Provides static methods for obtaining asset files in a Free Realms client directory.
/// </summary>
public static partial class ClientDirectory
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

    [GeneratedRegex(@"^Assets_\d{3}\.dat$", Options, "en-US")]
    private static partial Regex GameDataRegex();
    [GeneratedRegex(@"^assetpack000_\d{3}\.dat$", Options, "en-US")]
    private static partial Regex TcgDataRegex();
    [GeneratedRegex(@"^AssetsTcg_\d{3}\.dat$", Options, "en-US")]
    private static partial Regex ResourceDataRegex();

    /// <summary>
    /// Returns an enumerable collection of the asset files that match a filter on a specified path.
    /// </summary>
    /// <returns>
    /// An enumerable collection of the asset files in the directory specified
    /// by <paramref name="path"/> that match the specified filter.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<AssetFile> EnumerateAssetFiles(string path, AssetType assetFilter = AssetType.All)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));

        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            AssetType assetType = ClientFile.InferAssetType(file);

            if (assetType != 0 && assetFilter.HasFlag(assetType))
            {
                yield return new AssetFile(file, assetType);
            }
        }
    }

    /// <summary>
    /// Returns the asset files that match a filter on a specified path.
    /// </summary>
    /// <returns>
    /// An array of the asset files in the directory specified by
    /// <paramref name="path"/> that match the specified filter.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public static AssetFile[] GetAssetFiles(string path, AssetType assetFilter = AssetType.All)
        => EnumerateAssetFiles(path, assetFilter).ToArray();

    /// <summary>
    /// Returns an enumerable collection of full file names for all asset
    /// .dat files with the specified asset type in a specified path.
    /// </summary>
    /// <returns>
    /// An enumerable collection of the full file names (including paths) for the asset .dat files
    /// with the specified asset type in the directory specified by <paramref name="path"/>.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<string> EnumerateDataFiles(string path, AssetType assetType)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (!assetType.IsValid()) throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, assetType));
        if (assetType.GetFileType() != AssetType.Dat) return Enumerable.Empty<string>();

        Regex dataRegex = assetType.GetDirectoryType() switch
        {
            AssetType.Game => GameDataRegex(),
            AssetType.Tcg => TcgDataRegex(),
            AssetType.Resource => ResourceDataRegex(),
            _ => throw new InvalidEnumArgumentException(nameof(assetType), (int)assetType, assetType.GetType())
        };
        return Directory.EnumerateFiles(path)
                        .Where(x => dataRegex.IsMatch(Path.GetFileName(x.AsSpan())));
    }

    /// <summary>
    /// Returns the full file names of all asset .dat files with the specified asset type in a specified path.
    /// </summary>
    /// <returns>
    /// An array of full file names (including paths) for the asset .dat files with the
    /// specified asset type in the directory specified by <paramref name="path"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public static string[] GetDataFiles(string path, AssetType assetType)
        => EnumerateDataFiles(path, assetType).ToArray();
}

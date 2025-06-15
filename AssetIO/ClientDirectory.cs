namespace AssetIO;

/// <summary>
/// Provides static methods for obtaining asset files in a Free Realms client directory.
/// </summary>
public static class ClientDirectory
{
    private const string ManifestFileSuffix = "_manifest.dat";

    /// <summary>
    /// Returns an enumerable collection of the asset files that match a filter on a specified path.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="assetFilter">
    /// A bitwise combination of the enumeration values that specify which asset types are allowed.
    /// </param>
    /// <param name="searchOption">
    /// One of the enumeration values that specifies whether the search operation
    /// should include only the current directory or should include all subdirectories.
    /// </param>
    /// <param name="requireFullType">
    /// <see langword="true"/> to exclude asset files without a full asset type;
    /// <see langword="false"/> to include asset files with only a file type.
    /// </param>
    /// <returns>
    /// An enumerable collection of the asset files in the directory specified
    /// by <paramref name="path"/> that match the specified filter.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<AssetFile> EnumerateAssetFiles(string path,
                                                             AssetType assetFilter = AssetType.All,
                                                             SearchOption searchOption = SearchOption.AllDirectories,
                                                             bool requireFullType = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        foreach (string file in Directory.EnumerateFiles(path, "*", searchOption))
        {
            AssetType assetType = ClientFile.InferAssetType(file, requireFullType);

            if (assetType != 0 && (assetFilter & assetType) == assetType)
            {
                yield return new AssetFile(file, assetType);
            }
        }
    }

    /// <summary>
    /// Returns an enumerable collection of full file names for all asset
    /// .dat files with the specified asset type in a specified directory.
    /// </summary>
    /// <param name="directory">The directory to search.</param>
    /// <param name="assetFilter">
    /// A bitwise combination of the enumeration values that specify which asset types are allowed.
    /// </param>
    /// <param name="searchOption">
    /// One of the enumeration values that specifies whether the search operation
    /// should include only the current directory or should include all subdirectories.
    /// </param>
    /// <returns>
    /// An enumerable collection of the full file names (including paths) for the asset .dat
    /// files with the specified asset type in the specified <paramref name="directory"/>.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<string> EnumerateDataFiles(DirectoryInfo directory,
                                                         AssetType assetFilter = AssetType.All,
                                                         SearchOption searchOption = SearchOption.AllDirectories)
    {
        ArgumentNullException.ThrowIfNull(directory);

        foreach (string file in Directory.EnumerateFiles(directory.FullName, "*", searchOption))
        {
            AssetType assetType = ClientFile.InferDataType(file);

            if (assetType != 0 && (assetFilter & assetType) == assetType)
            {
                yield return file;
            }
        }
    }

    /// <inheritdoc cref="EnumerateDataFiles(FileInfo)"/>
    public static IEnumerable<string> EnumerateDataFiles(string manifestFile)
        => EnumerateDataFilesInfinite(manifestFile).TakeWhile(File.Exists);

    /// <summary>
    /// Returns an enumerable collection of full file names for all asset
    /// .dat files corresponding to the specified manifest.dat file.
    /// </summary>
    /// <param name="manifestFile">The manifest.dat file.</param>
    /// <returns>
    /// An enumerable collection of the full file names (including paths) for
    /// the asset .dat files corresponding to the specified manifest.dat file.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<string> EnumerateDataFiles(FileInfo manifestFile)
        => EnumerateDataFilesInfinite(manifestFile).TakeWhile(File.Exists);

    /// <inheritdoc cref="EnumerateDataFilesInfinite(FileInfo, int)"/>
    public static IEnumerable<string> EnumerateDataFilesInfinite(string manifestFile, int index = 0)
        => EnumerateDataFilesInfinite(new FileInfo(manifestFile), index);

    /// <summary>
    /// Returns an infinite enumerable of full file names for all asset
    /// .dat files corresponding to the specified manifest.dat file.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="EnumerateDataFiles(string)"/>, this method includes nonexistent files.
    /// </remarks>
    /// <param name="manifestFile">The manifest.dat file.</param>
    /// <param name="index">The starting index.</param>
    /// <returns>
    /// An infinite enumerable of the full file names (including paths) for
    /// the asset .dat files corresponding to the specified manifest.dat file.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<string> EnumerateDataFilesInfinite(FileInfo manifestFile, int index = 0)
    {
        ArgumentNullException.ThrowIfNull(manifestFile);

        string path = EscapeFormatString(manifestFile.FullName);
        string dataFileFormat = path.EndsWith(ManifestFileSuffix, StringComparison.OrdinalIgnoreCase)
            ? $"{path[..^ManifestFileSuffix.Length]}_{{0:D3}}.dat"
            : $"{Path.ChangeExtension(path, null)}_{{0:D3}}.dat";

        while (true)
        {
            yield return string.Format(dataFileFormat, index++);
        }
    }

    /// <summary>
    /// Escapes literal braces ({, }) by doubling them in the result string.
    /// </summary>
    /// <returns>A string of characters with brace characters converted to their escaped form.</returns>
    private static string EscapeFormatString(string str)
    {
        int i = str.IndexOfAny(['{', '}']);

        if (i < 0) return str;

        Span<char> result = str.Length <= 128 ? stackalloc char[2 * str.Length] : new char[2 * str.Length];

        // Copy the characters already proven not to match.
        if (i > 0)
        {
            str.AsSpan(0, i).CopyTo(result);
        }

        // Copy the remaining characters, doing the replacement as we go.
        for (int j = i; i < str.Length; i++)
        {
            char c = str[i];

            if (str[i] is '{' or '}')
            {
                result[j++] = c;
            }

            result[j++] = c;
        }

        return new string(result);
    }
}

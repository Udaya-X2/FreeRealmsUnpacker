namespace AssetIO
{
    /// <summary>
    /// Provides static methods for obtaining asset paths in a Free Realms client directory.
    /// </summary>
    public static class ClientDirectory
    {
        /// <summary>
        /// Returns a sorted mapping from asset directory type to full file names for assets in a specified path.
        /// </summary>
        /// <returns>A sorted mapping from asset directory type to the corresponding asset files.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static IDictionary<AssetType, List<string>> GetAssetFilesByType(string path)
            => GetAssetFilesByType(path, AssetType.All);

        /// <summary>
        /// Returns a sorted mapping from asset directory type to full file
        /// names for assets that match a filter on a specified path.
        /// </summary>
        /// <returns>A sorted mapping from asset directory type to the corresponding asset files.</returns>
        /// <exception cref="ArgumentNullException"/>
        public static IDictionary<AssetType, List<string>> GetAssetFilesByType(string path, AssetType assetFilter)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            SortedDictionary<AssetType, List<string>> assetTypeToFiles = new();

            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                AssetType assetType = ClientFile.GetAssetType(file);

                if (assetType != 0 && assetFilter.HasFlag(assetType))
                {
                    AssetType assetDirectoryType = assetType & AssetType.AllDirectories;

                    if (assetTypeToFiles.TryGetValue(assetDirectoryType, out List<string>? assetFiles))
                    {
                        assetFiles.Add(file);
                    }
                    else
                    {
                        assetTypeToFiles[assetDirectoryType] = new() { file };
                    }
                }
            }

            return assetTypeToFiles;
        }

        /// <summary>
        /// Returns the names of asset files (including their paths) in the specified directory.
        /// </summary>
        /// <returns>
        /// An array of the full file names (including paths) for the asset files
        /// in the specified directory, or an empty array if no files are found.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        public static string[] GetAssetFiles(string path)
            => EnumerateAssetFiles(path).ToArray();

        /// <summary>
        /// Returns the names of asset files (including their paths)
        /// that match the specified filter in the specified directory.
        /// </summary>
        /// <returns>
        /// An array of the full file names (including paths) for the asset files that match
        /// the filter in the specified directory, or an empty array if no files are found.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        public static string[] GetAssetFiles(string path, AssetType assetFilter)
            => EnumerateAssetFiles(path, assetFilter).ToArray();

        /// <summary>
        /// Returns an enumerable collection of full file names for assets in the specified path.
        /// </summary>
        /// <returns>
        /// An enumerable collection of the full file names (including paths) for
        /// the asset files in the directory specified by <paramref name="path"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        public static IEnumerable<string> EnumerateAssetFiles(string path)
            => EnumerateAssetFiles(path, AssetType.All);

        /// <summary>
        /// Returns an enumerable collection of full file names for assets that match a filter on a specified path.
        /// </summary>
        /// <returns>
        /// An enumerable collection of the full file names (including paths) for the asset files in
        /// the directory specified by <paramref name="path"/> and that match the specified filter.
        /// </returns>
        /// <exception cref="ArgumentNullException"/>
        public static IEnumerable<string> EnumerateAssetFiles(string path, AssetType assetFilter)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                AssetType assetType = ClientFile.GetAssetType(file);

                if (assetType != 0 && assetFilter.HasFlag(assetType))
                {
                    yield return file;
                }
            }
        }
    }
}

using AssetIO;
using Force.Crc32;
using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace UnpackerCli
{
    /// <summary>
    /// The driver class of the command line Free Realms Unpacker.
    /// </summary>
    public partial class Unpacker
    {
        private const string ManifestPattern = "Asset*_manifest.dat";
        private const string PackPattern = "Asset*.pack";

        /// <summary>
        /// The entry point of the command line Free Realms Unpacker, following command parsing.
        /// </summary>
        /// <returns>The process exit code.</returns>
        public int OnExecute()
        {
            try
            {
                // Validate the command line arguments.
                if (ListAssets && ValidateAssets) throw new Exception("Cannot both list and validate assets.");
                if (ListAssets && CountAssets) throw new Exception("Cannot both list and count assets.");
                if (ValidateAssets && CountAssets) throw new Exception("Cannot both validate and count assets.");
                if (!ListAssets && !ValidateAssets && !CountAssets && !SetupOutputDirectory()) return 1;

                // Handle the asset types specified.
                Stopwatch sw = Stopwatch.StartNew();
                int numAssets = 0;

                foreach (KeyValuePair<AssetType, List<string>> item in GetAssetFilesByType())
                {
                    numAssets += HandleAssets(item.Key, item.Value);
                }

                // Print the number of assets found.
                if (numAssets == 0)
                {
                    Console.Error.WriteLine("\nNo assets found.");
                }
                else
                {
                    Console.Error.WriteLine($"\n{numAssets} assets found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(Debug ? $"\n{ex}" : $"ERROR - {ex.Message}");
                Console.ResetColor();
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// Dispatches on assets of the specified type, based on the command.
        /// </summary>
        /// <returns>The number of assets of the specified type.</returns>
        private int HandleAssets(AssetType assetType, List<string> assetFiles)
        {
            if (assetFiles.Count == 0) return 0;
            if (CountAssets) return assetFiles.Sum(ClientDirectory.GetAssetCount);

            int numAssets = ListAssets || NoProgressBars ? 0 : assetFiles.Sum(ClientDirectory.GetAssetCount);
            using ProgressBar? pbar = numAssets > 0 ? CreateProgressBar(numAssets, assetType) : null;

            foreach (string assetFile in assetFiles)
            {
                Asset[] clientAssets = ClientDirectory.GetAssets(assetFile);

                if (ListAssets)
                {
                    Array.ForEach(clientAssets, asset => Console.WriteLine(asset.Name));
                    numAssets += clientAssets.Length;
                }
                else if (ValidateAssets)
                {
                    ValidateChecksums(assetFile, clientAssets, pbar);
                }
                else
                {
                    ExtractAssets(assetFile, clientAssets, pbar);
                }
            }

            return numAssets;
        }

        /// <summary>
        /// Validates assets of the specified type by comparing their checksums.
        /// </summary>
        private static void ValidateChecksums(string assetFile, Asset[] clientAssets, ProgressBar? pbar)
        {
            if (clientAssets.Length == 0) return;

            using AssetPackReader reader = new(assetFile);
            byte[] buffer = new byte[clientAssets.Max(x => x.Size)];
            int numErrors = 0;

            foreach (Asset asset in clientAssets)
            {
                reader.Read(asset, buffer);

                if (asset.Crc32 == Crc32Algorithm.Compute(buffer, 0, asset.Size))
                {
                    pbar?.UpdateProgress($"[{numErrors} CRC errors] Validated {asset.Name}");
                }
                else if (pbar != null)
                {
                    pbar.UpdateProgress($"[{++numErrors} CRC errors] Validated {asset.Name}");
                }
                else
                {
                    Console.WriteLine($"{asset.Name} does not match the expected CRC.");
                }
            }
        }

        /// <summary>
        /// Extracts assets of the specified type to the output directory.
        /// </summary>
        private void ExtractAssets(string assetFile, Asset[] clientAssets, ProgressBar? pbar)
        {
            if (clientAssets.Length == 0) return;

            using AssetPackReader reader = new(assetFile);
            byte[] buffer = new byte[clientAssets.Max(x => x.Size)];
            CreateDirectoryStructure(clientAssets);

            foreach (Asset asset in clientAssets)
            {
                string assetPath = $"{OutputDirectory}/{asset.Name}";

                if (SkipExisting && File.Exists(assetPath))
                {
                    pbar?.UpdateProgress($"Skipped {asset.Name}");
                }
                else
                {
                    reader.Read(asset, buffer);
                    using FileStream fs = new(assetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    fs.Write(buffer, 0, asset.Size);
                    pbar?.UpdateProgress($"Extracted {asset.Name}");
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="ProgressBar"/> with the specified number of assets
        /// for max ticks and an <paramref name="assetType"/>-dependent color.
        /// </summary>
        /// <returns>
        /// A <see cref="ProgressBar"/> with the number of assets for max
        /// ticks and an <paramref name="assetType"/>-dependent color.
        /// </returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        private static ProgressBar CreateProgressBar(int numAssets, AssetType assetType)
        {
            (string message, ConsoleColor color) = assetType switch
            {
                AssetType.Game => ("Reading game assets...", ConsoleColor.Green),
                AssetType.Tcg => ("Reading TCG assets...", ConsoleColor.Blue),
                AssetType.Resource => ("Reading resource assets...", ConsoleColor.DarkYellow),
                _ => throw new InvalidEnumArgumentException(nameof(assetType), (int)assetType, assetType.GetType())
            };

            ProgressBarOptions options = new() { ForegroundColor = color, ProgressCharacter = '─' };
            return new ProgressBar(numAssets, message, options);
        }

        /// <summary>
        /// Creates the directory structure used by the client assets in the output directory.
        /// </summary>
        private void CreateDirectoryStructure(Asset[] clientAssets)
        {
            foreach (string? assetDir in clientAssets.Select(x => Path.GetDirectoryName(x.Name)).Distinct())
            {
                Directory.CreateDirectory($"{OutputDirectory}/{assetDir}");
            }
        }

        /// <summary>
        /// Scans the input directory for asset files that match the command-line extraction options.
        /// </summary>
        /// <returns>A sorted mapping from asset type to the corresponding asset files.</returns>
        private IDictionary<AssetType, List<string>> GetAssetFilesByType()
        {
            // Create a mapping from asset type to asset files.
            SortedDictionary<AssetType, List<string>> assetFiles = new();
            Array.ForEach((AssetType[])Enum.GetValues(typeof(AssetType)), x => assetFiles[x] = new());
            GetExtractionOptions(out bool extractGame, out bool extractTcg, out bool extractResource);

            // Create patterns to determine the asset type of each file.
            Regex gameAssetRegex = new(@"^Assets(_ps3)?W?_(manifest\.dat|\d{3}\.pack)$", RegexOptions.IgnoreCase);
            Regex tcgAssetRegex = new(@"^assetpack000W?_(manifest\.dat|\d{3}\.pack)$", RegexOptions.IgnoreCase);
            Regex resourceAssetRegex = new(@"^AssetsTcgW?_(manifest\.dat|\d{3}\.pack)$", RegexOptions.IgnoreCase);

            // Bin each asset path into an asset type based on the filename.
            foreach (string path in GetAssetFiles())
            {
                string filename = Path.GetFileName(path);

                if (extractGame && gameAssetRegex.IsMatch(filename))
                {
                    assetFiles[AssetType.Game].Add(path);
                }
                else if (extractTcg && tcgAssetRegex.IsMatch(filename))
                {
                    assetFiles[AssetType.Tcg].Add(path);
                }
                else if (extractResource && resourceAssetRegex.IsMatch(filename))
                {
                    assetFiles[AssetType.Resource].Add(path);
                }
            }

            return assetFiles;
        }

        /// <summary>
        /// Initializes the specified asset type extraction options according to the command-line arguments.
        /// </summary>
        private void GetExtractionOptions(out bool extractGame, out bool extractTcg, out bool extractResource)
        {
            // By default (no options are set), extract all asset types.
            extractGame = ExtractGame || !(ExtractTcg || ExtractResource);
            extractTcg = ExtractTcg || !(ExtractGame || ExtractResource);
            extractResource = ExtractResource || !(ExtractGame || ExtractTcg);
        }

        /// <summary>
        /// Returns an enumerable collection of full asset file names that match the command-line extraction options.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetAssetFiles() => (ExtractDat, ExtractPack) switch
        {
            (true, false) => Directory.EnumerateFiles(InputDirectory, ManifestPattern, SearchOption.AllDirectories),
            (false, true) => Directory.EnumerateFiles(InputDirectory, PackPattern, SearchOption.AllDirectories),
            // By default (no options are set), extract both .dat and .pack assets.
            _ => Directory.EnumerateFiles(InputDirectory, "*", SearchOption.AllDirectories)
                          .Where(IsAssetPackOrManifestFile)
        };

        /// <summary>
        /// Determines whether the specified path string is an asset pack or manifest file.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/> is the name of
        /// an asset pack or manifest file; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool IsAssetPackOrManifestFile(string path)
        {
            ReadOnlySpan<char> filename = Path.GetFileName(path.AsSpan());
            return filename.StartsWith("Asset", StringComparison.OrdinalIgnoreCase)
                && (filename.EndsWith(".pack", StringComparison.OrdinalIgnoreCase)
                || filename.EndsWith("_manifest.dat", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Prompts the user to create the output directory if it does not exist.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the output directory was set up successfully; otherwise, <see langword="false"/>.
        /// </returns>
        private bool SetupOutputDirectory()
        {
            DirectoryInfo dir = new(OutputDirectory);

            if (!dir.Exists)
            {
                if (PromptUser($"{dir.FullName} does not exist. Create the directory?"))
                {
                    dir.Create();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a yes/no response from the console after displaying a <paramref name="prompt"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the answer is 'yes'; otherwise, <see langword="false"/>.</returns>
        private bool PromptUser(string prompt) => AnswerYes || Prompt.GetYesNo(prompt, true);
    }
}

using AssetIO;
using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;
using System.ComponentModel;
using System.Diagnostics;

namespace UnpackerCli
{
    /// <summary>
    /// The driver class of the command line Free Realms Unpacker.
    /// </summary>
    public partial class Unpacker
    {
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
                AssetType assetFilter = GetAssetFilter();
                int numAssets = 0;

                foreach (var item in ClientDirectory.GetAssetFilesByType(InputDirectory, assetFilter))
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

            using ProgressBar? pbar = CreateProgressBar(assetFiles, assetType);
            int numAssets = 0;
            int numErrors = 0;

            foreach (string assetFile in assetFiles)
            {
                Asset[] assets = ClientDirectory.GetAssets(assetFile);

                if (ListAssets)
                {
                    Array.ForEach(assets, asset => Console.WriteLine(asset.Name));
                }
                else if (ValidateAssets)
                {
                    ValidateChecksums(assetFile, assets, pbar, ref numErrors);
                }
                else
                {
                    ExtractAssets(assetFile, assets, pbar);
                }

                numAssets += assets.Length;
            }

            return numAssets;
        }

        /// <summary>
        /// Validates assets in the specified asset file by comparing their checksums.
        /// </summary>
        private static void ValidateChecksums(string assetFile, Asset[] assets, ProgressBar? pbar, ref int numErrors)
        {
            if (assets.Length == 0) return;

            using AssetReader reader = AssetReader.Create(assetFile);

            foreach (Asset asset in assets)
            {
                if (asset.Crc32 == reader.GetCrc32(asset))
                {
                    pbar?.UpdateProgress($"[{numErrors} CRC errors] Validated {asset.Name}");
                }
                else if (pbar != null)
                {
                    pbar.UpdateProgress($"[{++numErrors} CRC errors] Validated {asset.Name}");
                }
                else
                {
                    Console.WriteLine($"'{asset.Name}' in '{assetFile}' does not match the expected CRC.");
                }
            }
        }

        /// <summary>
        /// Extracts assets in the specified asset file to the output directory.
        /// </summary>
        private void ExtractAssets(string assetFile, Asset[] clientAssets, ProgressBar? pbar)
        {
            if (clientAssets.Length == 0) return;

            using AssetReader reader = AssetReader.Create(assetFile);

            foreach (Asset asset in clientAssets)
            {
                string assetPath = $"{OutputDirectory}/{asset.Name}";

                if (SkipExisting && File.Exists(assetPath))
                {
                    pbar?.UpdateProgress($"Skipped {asset.Name}");
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? OutputDirectory);
                    using FileStream fs = new(assetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    reader.CopyTo(asset, fs);
                    pbar?.UpdateProgress($"Extracted {asset.Name}");
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="ProgressBar"/> to display the number of
        /// assets processed in the asset files of the specified type.
        /// </summary>
        /// <returns>
        /// A <see cref="ProgressBar"/> with the number of assets for max
        /// ticks and an <paramref name="assetType"/>-dependent color,
        /// or <see langword="null"/> if progress bars are disabled.
        /// </returns>
        /// <exception cref="InvalidEnumArgumentException"></exception>
        private ProgressBar? CreateProgressBar(IEnumerable<string> assetFiles, AssetType assetType)
        {
            if (NoProgressBars || ListAssets || CountAssets) return null;

            (string message, ConsoleColor color) = assetType switch
            {
                AssetType.Game => ("Reading game assets...", ConsoleColor.Green),
                AssetType.Tcg => ("Reading TCG assets...", ConsoleColor.Blue),
                AssetType.Resource => ("Reading resource assets...", ConsoleColor.DarkYellow),
                _ => throw new InvalidEnumArgumentException(nameof(assetType), (int)assetType, assetType.GetType())
            };
            int numAssets = assetFiles.Sum(ClientDirectory.GetAssetCount);
            ProgressBarOptions options = new() { ForegroundColor = color, ProgressCharacter = '─' };
            return new ProgressBar(numAssets, message, options);
        }

        /// <summary>
        /// Returns an enum value specifying the asset types to extract, based on the command-line options.
        /// </summary>
        /// <returns>An enum value specifying the asset types to extract.</returns>
        private AssetType GetAssetFilter()
        {
            Span<bool> options = stackalloc[] { ExtractGame, ExtractTcg, ExtractResource, ExtractDat, ExtractPack };
            AssetType extractionOptions = 0;

            // Set the enum flag corresponding to each command-line option.
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i])
                {
                    extractionOptions |= (AssetType)(1 << i);
                }
            }

            // By default (no options are set), extract all asset types.
            if ((extractionOptions & AssetType.AllDirectories) == 0)
            {
                extractionOptions |= AssetType.AllDirectories;
            }
            if ((extractionOptions & AssetType.AllFiles) == 0)
            {
                extractionOptions |= AssetType.AllFiles;
            }

            return extractionOptions;
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

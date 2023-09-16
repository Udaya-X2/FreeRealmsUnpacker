using Force.Crc32;
using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;
using System.Data;
using System.Diagnostics;

namespace FreeRealmsUnpacker
{
    /// <summary>
    /// The driver class of <see href="FreeRealmsUnpacker"/>.
    /// </summary>
    public partial class Unpacker
    {
        /// <summary>
        /// The entry point of <see href="FreeRealmsUnpacker"/>, following command parsing.
        /// </summary>
        /// <returns>The process exit code.</returns>
        public int OnExecute()
        {
            try
            {
                // Validate the command line arguments.
                if (ListAssets && ValidateAssets) throw new Exception("Cannot both list and validate assets.");
                if (!ListAssets && !ValidateAssets && !SetupOutputDirectory()) return 1;

                // By default, handle all assets.
                Stopwatch sw = Stopwatch.StartNew();
                bool extractAll = !ExtractGame && !ExtractTcg && !ExtractResource;
                int numAssets = 0;

                // Handle the asset types specified.
                if (extractAll || ExtractGame) numAssets += HandleAssets(AssetType.Game);
                if (extractAll || ExtractTcg) numAssets += HandleAssets(AssetType.Tcg);
                if (extractAll || ExtractResource) numAssets += HandleAssets(AssetType.Resource);

                // Print the number of assets found.
                if (numAssets == 0)
                {
                    Console.Error.WriteLine("\nNo assets found.");
                }
                else
                {
                    Console.Error.WriteLine($"\n{numAssets} assets found in {sw.Elapsed:hh\\:mm\\:ss\\.ff}");
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
        /// Lists or extracts assets of the specified type, depending on the command.
        /// </summary>
        /// <returns>The number of assets of the specified type.</returns>
        private int HandleAssets(AssetType assetType)
        {
            Asset[] clientAssets = ManifestReader.GetClientAssets(InputDirectory, assetType);

            if (ListAssets)
            {
                Array.ForEach(clientAssets, asset => Console.WriteLine(asset.Name));
            }
            else if (ValidateAssets)
            {
                ValidateChecksums(clientAssets, assetType);
            }
            else
            {
                ExtractAssets(clientAssets, assetType);
            }

            return clientAssets.Length;
        }

        /// <summary>
        /// Validates assets of the specified type by comparing their checksums.
        /// </summary>
        private void ValidateChecksums(Asset[] clientAssets, AssetType assetType)
        {
            if (clientAssets.Length == 0) return;

            byte[] buffer = new byte[clientAssets.Max(x => x.Size)];
            using AssetPackReader reader = new(InputDirectory, assetType);
            using ProgressBar? pbar = GetExtractionProgressBar(clientAssets.Length, assetType);
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
        /// <exception cref="InvalidDataException"></exception>
        private void ExtractAssets(Asset[] clientAssets, AssetType assetType)
        {
            if (clientAssets.Length == 0) return;

            byte[] buffer = new byte[clientAssets.Max(x => x.Size)];
            using AssetPackReader reader = new(InputDirectory, assetType);
            using ProgressBar? pbar = GetExtractionProgressBar(clientAssets.Length, assetType);
            CreateDirectoryStructure(clientAssets, assetType);

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
        /// <exception cref="ArgumentException"></exception>
        private ProgressBar? GetExtractionProgressBar(int numAssets, AssetType assetType) => assetType switch
        {
            AssetType.Game => CreateProgressBar(numAssets, "Reading game assets...", ConsoleColor.Green),
            AssetType.Tcg => CreateProgressBar(numAssets, "Reading TCG assets...", ConsoleColor.Blue),
            AssetType.Resource => CreateProgressBar(numAssets, "Reading resource assets...", ConsoleColor.DarkYellow),
            _ => throw new ArgumentException("Invalid enum value for asset type", nameof(assetType))
        };

        /// <summary>
        /// Creates a <see cref="ProgressBar"/> with the specified parameters.
        /// </summary>
        /// <returns>
        /// A <see cref="ProgressBar"/> with the specified parameters,
        /// or <see langword="null"/> if progress bars are disabled.
        /// </returns>
        private ProgressBar? CreateProgressBar(int maxTicks, string message, ConsoleColor color)
        {
            ProgressBarOptions options = new() { ForegroundColor = color, ProgressCharacter = '─' };
            return NoProgressBars ? null : new ProgressBar(maxTicks, message, options);
        }

        /// <summary>
        /// Creates the directory structure used by the client assets in the output directory.
        /// </summary>
        private void CreateDirectoryStructure(Asset[] clientAssets, AssetType assetType)
        {
            // Only TCG assets require subdirectories.
            if (assetType == AssetType.Tcg)
            {
                foreach (string? assetDir in clientAssets.Select(x => Path.GetDirectoryName(x.Name)).Distinct())
                {
                    Directory.CreateDirectory($"{OutputDirectory}/{assetDir}");
                }
            }
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

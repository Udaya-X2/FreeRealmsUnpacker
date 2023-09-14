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
                // Set up the input and output directories.
                if (!ValidateInputDirectory() || !SetupOutputDirectory()) return 1;

                // By default, extract all assets.
                Stopwatch sw = Stopwatch.StartNew();
                bool extractAll = !ExtractGame && !ExtractTcg && !ExtractResource;
                int numAssets = 0;

                // Handle the asset type(s) specified.
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
            else
            {
                ExtractAssets(clientAssets, assetType);
            }

            return clientAssets.Length;
        }

        /// <summary>
        /// Extracts assets of the specified type to the output directory.
        /// </summary>
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
                    pbar?.Tick($"({pbar.CurrentTick + 1})/({pbar.MaxTicks}) Skipped {asset.Name}");
                }
                else
                {
                    reader.Read(asset, buffer);
                    using FileStream assetWriter = File.Open(assetPath, FileMode.Create);
                    assetWriter.Write(buffer, 0, asset.Size);
                    pbar?.Tick($"({pbar.CurrentTick + 1})/({pbar.MaxTicks}) Extracted {asset.Name}");
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
            _ => throw new ArgumentException("Invalid enum value for extraction type", nameof(assetType))
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
                foreach (string? assetDir in clientAssets.Select(x => Path.GetDirectoryName(x.Name)).ToHashSet())
                {
                    Directory.CreateDirectory($"{OutputDirectory}/{assetDir ?? ""}");
                }
            }
        }

        /// <summary>
        /// Checks whether the input directory exists.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the input directory exists; otherwise, <see langword="false"/>.
        /// </returns>
        private bool ValidateInputDirectory()
        {
            if (!Directory.Exists(InputDirectory))
            {
                Console.Error.WriteLine($"{InputDirectory} is not a directory.");
                return false;
            }

            return true;
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

            // Skip the directory setup if assets are being listed instead of extracted.
            if (!ListAssets && !dir.Exists)
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

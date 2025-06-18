using AssetIO;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace UnpackerCli;

public partial class Unpacker
{
    /// <summary>
    /// Gets the Free Realms client directory or asset file.
    /// </summary>
    [Argument(0, Name = "InputFile/Directory", Description = "The Free Realms asset file or client directory.")]
    [Required]
    [FileOrDirectoryExists]
    public string InputFile { get; } = "";

    /// <summary>
    /// Gets the destination for extracted assets.
    /// </summary>
    [Argument(1, Description = "The destination for extracted assets.")]
    [Required]
    [FileNotExists]
    public string OutputDirectory { get; } = "./assets";

    /// <summary>
    /// Gets whether to extract game assets.
    /// </summary>
    [Option(ShortName = "g", Description = "Extract game assets only (in 'Free Realms/').")]
    public bool ExtractGame { get; }

    /// <summary>
    /// Gets whether to extract TCG assets.
    /// </summary>
    [Option(ShortName = "t", Description = "Extract TCG assets only (in 'Free Realms/assets/').")]
    public bool ExtractTcg { get; }

    /// <summary>
    /// Gets whether to extract resource assets.
    /// </summary>
    [Option(ShortName = "r", Description = "Extract resource assets only (in 'Free Realms/tcg/').")]
    public bool ExtractResource { get; }

    /// <summary>
    /// Gets whether to extract PS3 assets.
    /// </summary>
    [Option(ShortName = "3", Description = "Extract PS3 assets only (in 'NPUA30048/USRDIR/' or 'NPEA00299/USRDIR').")]
    public bool ExtractPS3 { get; }

    /// <summary>
    /// Gets whether to extract unknown assets.
    /// </summary>
    [Option(ShortName = "u", Description = "Extract unknown assets only (disabled by default).")]
    public bool ExtractUnknown { get; }

    /// <summary>
    /// Gets whether to extract .pack assets only.
    /// </summary>
    [Option(ShortName = "p", Description = "Extract .pack assets only.")]
    public bool ExtractPack { get; }

    /// <summary>
    /// Gets whether to extract .dat assets only.
    /// </summary>
    [Option(ShortName = "d", Description = "Extract .dat assets only.")]
    public bool ExtractDat { get; }

    /// <summary>
    /// Gets whether to ignore .temp asset files.
    /// </summary>
    [Option(ShortName = "i", Description = "Ignore .temp asset files.")]
    public bool IgnoreTemp { get; }

    /// <summary>
    /// Gets the output file, or <see langword="null"/> if not writing
    /// assets from the input file or directory to an asset file.
    /// </summary>
    [Option(ShortName = "w", LongName = "write-assets", ValueName = "FILE",
        Description = "Write the assets from the input file or directory to an asset file.\n"
                      + "The input file should contain a list of paths separated by newlines.\n"
                      + "The input directory should contain the assets to add to the file.")]
    [DirectoryNotExists]
    public string? OutputFile { get; }

    /// <summary>
    /// Gets whether to append assets instead of overwriting the asset file.
    /// </summary>
    [Option(ShortName = "a", Description = "Append assets instead of overwriting the asset file.\n"
                                           + "Requires --write-assets.")]
    public bool AppendAssets { get; }

    /// <summary>
    /// Gets whether to list the assets without extracting them.
    /// </summary>
    [Option(ShortName = "l", Description = "List the assets without extracting them.")]
    public bool ListAssets { get; }

    /// <summary>
    /// Gets whether to list the asset file paths without extracting them.
    /// </summary>
    [Option(ShortName = "f", Description = "List the asset file paths without extracting them.")]
    public bool ListFiles { get; }

    /// <summary>
    /// Gets whether to validate the assets without extracting them.
    /// </summary>
    [Option(ShortName = "v", Description = "Validate the assets without extracting them.")]
    public bool ValidateAssets { get; }

    /// <summary>
    /// Gets whether to count the assets without extracting them.
    /// </summary>
    [Option(ShortName = "c", Description = "Count the assets without extracting them.")]
    public bool CountAssets { get; }

    /// <summary>
    /// Gets whether to display listed information as comma-separated values.
    /// </summary>
    [Option(ShortName = "C", Description = "Display listed information as comma-separated values.\n"
                                           + "Requires --list-assets or --list-files.")]
    public bool DisplayCsv { get; }

    /// <summary>
    /// Gets whether to display listed information in a table.
    /// </summary>
    [Option(ShortName = "#", Description = "Display listed information in a table.\n"
                                           + "Requires --list-assets or --list-files.")]
    public bool DisplayTable { get; }

    /// <summary>
    /// Gets how to handle assets with conflicting names.
    /// </summary>
    [Option(ShortName = "H", ValueName = "MODE", Description = "Specify how to handle assets with conflicting names.")]
    public FileConflictOptions HandleConflicts { get; }

    /// <summary>
    /// Gets whether to disable progress bars.
    /// </summary>
    [Option(ShortName = "n", Description = "Do not show progress bars.")]
    public bool NoProgressBars { get; }

    /// <summary>
    /// Gets whether to automatically answer yes to any question.
    /// </summary>
    [Option(ShortName = "y", Description = "Automatically answer yes to any question.")]
    public bool AnswerYes { get; }

    /// <summary>
    /// Gets whether to show complete exception stack traces.
    /// </summary>
    [Option(ShortName = "D", Description = "Show complete exception stack traces.")]
    public bool Debug { get; }
}

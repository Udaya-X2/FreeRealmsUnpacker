using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace UnpackerCli;

public partial class Unpacker
{
    /// <summary>
    /// Gets the Free Realms client directory or asset file.
    /// </summary>
    [Argument(0, Name = "InputDirectory/AssetFile", Description = "The Free Realms client directory or asset file.")]
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
    /// Gets whether to extract unknown assets.
    /// </summary>
    [Option(ShortName = "u", Description = "Extract unknown assets only (disabled by default).")]
    public bool ExtractUnknown { get; }

    /// <summary>
    /// Gets whether to extract .dat assets only.
    /// </summary>
    [Option(ShortName = "d", Description = "Extract .dat assets only.")]
    public bool ExtractDat { get; }

    /// <summary>
    /// Gets whether to extract .pack assets only.
    /// </summary>
    [Option(ShortName = "p", Description = "Extract .pack assets only.")]
    public bool ExtractPack { get; }

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
    [Option(ShortName = "C", Description = "Display listed information as comma-separated values.")]
    public bool DisplayCsv { get; }

    /// <summary>
    /// Gets whether to display listed information in a table.
    /// </summary>
    [Option(ShortName = "#", Description = "Display listed information in a table.")]
    public bool DisplayTable { get; }

    /// <summary>
    /// Gets how to handle assets with conflicting names.
    /// </summary>
    [Option(ShortName = "H", ValueName = "MODE", Description = "Specify how to handle assets with conflicting names.")]
    public ConflictOptions HandleConflicts { get; }

    /// <summary>
    /// Gets whether to disable progress bars.
    /// </summary>
    [Option(ShortName = "n", Description = "Don't show progress bars.")]
    public bool NoProgressBars { get; }

    /// <summary>
    /// Gets whether to automatically answer yes to any question.
    /// </summary>
    [Option(ShortName = "y", Description = "Automatically answer yes to any question.")]
    public bool AnswerYes { get; }

    /// <summary>
    /// Gets whether to show complete exception stack traces.
    /// </summary>
    [Option(ShortName = "e", Description = "Show complete exception stack traces.")]
    public bool Debug { get; }
}

namespace UnpackerCli;

/// <summary>
/// Specifies how to handle file conflicts.
/// </summary>
public enum ConflictOptions
{
    /// <summary>
    /// Overwrite the existing file.
    /// </summary>
    Overwrite = 0,
    /// <summary>
    /// Skip the file.
    /// </summary>
    Skip = 1,
    /// <summary>
    /// Add a number to the new file's name.
    /// </summary>
    Rename = 2,
    /// <summary>
    /// Create a directory with a number added to its name for the new file.
    /// </summary>
    MkDir = 3,
    /// <summary>
    /// Create a subdirectory with a distinct name for each file.
    /// </summary>
    MkSubdir = 4,
    /// <summary>
    /// Create a subdirectory with a directory for each file.
    /// </summary>
    MkTree = 5,
}

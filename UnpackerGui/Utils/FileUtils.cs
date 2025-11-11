using System;
using System.IO;

namespace UnpackerGui.Utils;

public static class FileUtils
{
    /// <summary>
    /// Locates the specified file by searching the current directory
    /// and paths specified in the PATH environment variable.
    /// </summary>
    /// <returns>
    /// The first location of the specified file, or <see langword="null"/> if the file cannot be found.
    /// </returns>
    public static string? Locate(string file)
    {
        string[] paths = [Environment.CurrentDirectory, .. GetPathStrings("PATH")];
        string[] extensions = ["", .. GetPathStrings("PATHEXT")];

        foreach (string path in paths)
        {
            foreach (string extension in extensions)
            {
                string filePath = Path.Combine(path, file + extension.ToLower());

                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the path strings for the specified environment variable, or an empty array if the variable is not found.
    /// </summary>
    /// <param name="value">The name of the environment variable.</param>
    /// <returns>The path strings.</returns>
    public static string[] GetPathStrings(string value)
        => Environment.GetEnvironmentVariable(value)?.Split(Path.PathSeparator, StringSplitOptions.TrimEntries) ?? [];
}

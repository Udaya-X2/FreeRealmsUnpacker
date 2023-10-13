using Avalonia.Platform.Storage;

namespace UnpackerGui.Storage;

public static class FilePickerTypes
{
    public static FilePickerFileType AllFiles { get; } = new("All Files")
    {
        Patterns = new[] { "*.*" },
        MimeTypes = new[] { "*/*" }
    };

    public static FilePickerFileType PackFiles => new("Pack Files")
    {
        Patterns = new[] { "*.pack" }
    };

    public static FilePickerFileType DatFiles => new("Dat Files")
    {
        Patterns = new[] { "*.dat" }
    };
}

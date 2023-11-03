using Avalonia.Platform.Storage;

namespace UnpackerGui.Storage;

public static class FilePickerTypes
{
    public static readonly FilePickerFileType AllFiles = new("All Files")
    {
        Patterns = new[] { "*.*" },
        MimeTypes = new[] { "*/*" }
    };

    public static readonly FilePickerFileType PackFiles = new("Pack Files")
    {
        Patterns = new[] { "*.pack" }
    };

    public static readonly FilePickerFileType ManifestFiles = new("Manifest Files")
    {
        Patterns = new[] { "*_manifest.dat" }
    };

    public static readonly FilePickerFileType AssetDatFiles = new("Asset Dat Files")
    {
        Patterns = new[] { "*_???.dat" }
    };
}

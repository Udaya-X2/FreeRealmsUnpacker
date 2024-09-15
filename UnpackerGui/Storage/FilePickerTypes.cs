using Avalonia.Platform.Storage;

namespace UnpackerGui.Storage;

public static class FilePickerTypes
{
    public static readonly FilePickerFileType AllFiles = new("All Files")
    {
        Patterns = ["*.*"],
        MimeTypes = ["*/*"]
    };

    public static readonly FilePickerFileType PackFiles = new("Pack Files")
    {
        Patterns = ["*.pack"]
    };

    public static readonly FilePickerFileType ManifestFiles = new("Manifest Files")
    {
        Patterns = ["*_manifest.dat"]
    };

    public static readonly FilePickerFileType AssetFiles = new("Asset Files")
    {
        Patterns = ["*.pack", "*_manifest.dat"]
    };

    public static readonly FilePickerFileType AssetDatFiles = new("Asset Dat Files")
    {
        Patterns = ["*_???.dat"]
    };
}

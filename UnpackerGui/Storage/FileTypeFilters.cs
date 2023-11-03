using Avalonia.Platform.Storage;

namespace UnpackerGui.Storage;

public static class FileTypeFilters
{
    public static readonly FilePickerFileType[] PackFiles = new FilePickerFileType[]
    {
        FilePickerTypes.PackFiles,
        FilePickerTypes.AllFiles
    };

    public static readonly FilePickerFileType[] ManifestFiles = new FilePickerFileType[]
    {
        FilePickerTypes.ManifestFiles,
        FilePickerTypes.AllFiles
    };

    public static readonly FilePickerFileType[] AssetDatFiles = new FilePickerFileType[]
    {
        FilePickerTypes.AssetDatFiles,
        FilePickerTypes.AllFiles
    };
}

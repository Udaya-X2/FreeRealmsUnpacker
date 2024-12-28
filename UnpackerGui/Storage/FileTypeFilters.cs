using Avalonia.Platform.Storage;

namespace UnpackerGui.Storage;

public static class FileTypeFilters
{
    public static readonly FilePickerFileType[] PackFiles =
    [
        FilePickerTypes.PackFiles,
        FilePickerTypes.AllFiles
    ];

    public static readonly FilePickerFileType[] ManifestFiles =
    [
        FilePickerTypes.ManifestFiles,
        FilePickerTypes.AllFiles
    ];

    public static readonly FilePickerFileType[] AssetFiles =
    [
        FilePickerTypes.AssetFiles,
        FilePickerTypes.PackFiles,
        FilePickerTypes.ManifestFiles,
        FilePickerTypes.PackTempFiles,
        FilePickerTypes.AllFiles
    ];

    public static readonly FilePickerFileType[] AssetDatFiles =
    [
        FilePickerTypes.AssetDatFiles,
        FilePickerTypes.AllFiles
    ];
}

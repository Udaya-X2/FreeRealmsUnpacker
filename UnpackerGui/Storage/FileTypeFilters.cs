using Avalonia.Platform.Storage;

namespace UnpackerGui.Storage;

public static class FileTypeFilters
{
    public static readonly FilePickerFileType[] PackFiles = new FilePickerFileType[]
    {
        FilePickerTypes.PackFiles,
        FilePickerTypes.AllFiles
    };

    public static readonly FilePickerFileType[] DatFiles = new FilePickerFileType[]
    {
        FilePickerTypes.DatFiles,
        FilePickerTypes.AllFiles
    };
}

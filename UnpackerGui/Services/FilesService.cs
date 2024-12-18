using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public class FilesService(TopLevel target) : IFilesService
{
    private readonly IStorageProvider _storageProvider = target.StorageProvider;

    public async Task<IStorageFile?> OpenFileAsync(FilePickerOpenOptions options)
    {
        IReadOnlyList<IStorageFile> files = await OpenFilesAsync(options);
        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions options)
        => await _storageProvider.OpenFilePickerAsync(options);

    public async Task<IStorageFolder?> OpenFolderAsync(FolderPickerOpenOptions options)
    {
        IReadOnlyList<IStorageFolder> folders = await OpenFoldersAsync(options);
        return folders.Count >= 1 ? folders[0] : null;
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions options)
        => await _storageProvider.OpenFolderPickerAsync(options);

    public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options)
        => await _storageProvider.SaveFilePickerAsync(options);

    public async Task<IStorageFolder?> TryGetFolderFromPath(string folderPath)
        => await _storageProvider.TryGetFolderFromPathAsync(folderPath);
}

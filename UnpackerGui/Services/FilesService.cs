using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public class FilesService : IFilesService
{
    private readonly Window _target;

    public FilesService(Window target)
    {
        _target = target;
    }

    public async Task<IStorageFile?> OpenFileAsync() => await OpenFileAsync(new()
    {
        Title = "Open File",
        AllowMultiple = false
    });

    public async Task<IStorageFile?> OpenFileAsync(FilePickerOpenOptions options)
    {
        IReadOnlyList<IStorageFile> files = await OpenFilesAsync(options);
        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilesAsync() => await OpenFilesAsync(new()
    {
        Title = "Open File",
        AllowMultiple = true
    });

    public async Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions options)
        => await _target.StorageProvider.OpenFilePickerAsync(options);

    public async Task<IStorageFolder?> OpenFolderAsync() => await OpenFolderAsync(new()
    {
        Title = "Open Folder",
        AllowMultiple = false
    });

    public async Task<IStorageFolder?> OpenFolderAsync(FolderPickerOpenOptions options)
    {
        IReadOnlyList<IStorageFolder> folders = await OpenFoldersAsync(options);
        return folders.Count >= 1 ? folders[0] : null;
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync() => await OpenFoldersAsync(new()
    {
        Title = "Open Folder",
        AllowMultiple = true
    });

    public async Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions options)
        => await _target.StorageProvider.OpenFolderPickerAsync(options);

    public async Task<IStorageFile?> SaveFileAsync() => await SaveFileAsync(new()
    {
        Title = "Save File As"
    });

    public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options)
        => await _target.StorageProvider.SaveFilePickerAsync(options);
}

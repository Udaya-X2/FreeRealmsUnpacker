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

    public async Task<IStorageFile?> OpenFileAsync() => await OpenFileAsync(new FilePickerOpenOptions
    {
        Title = "Open File",
        AllowMultiple = false
    });

    public async Task<IStorageFile?> OpenFileAsync(FilePickerOpenOptions options)
    {
        IReadOnlyList<IStorageFile> files = await OpenFilesAsync(options);
        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilesAsync() => await OpenFilesAsync(new FilePickerOpenOptions
    {
        Title = "Open File",
        AllowMultiple = true
    });

    public async Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions options)
        => await _target.StorageProvider.OpenFilePickerAsync(options);

    public async Task<IStorageFile?> SaveFileAsync() => await SaveFileAsync(new FilePickerSaveOptions
    {
        Title = "Save File As"
    });

    public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options)
        => await _target.StorageProvider.SaveFilePickerAsync(options);
}

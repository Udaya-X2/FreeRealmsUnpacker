using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public interface IFilesService
{
    public Task<IStorageFile?> OpenFileAsync();
    public Task<IStorageFile?> OpenFileAsync(FilePickerOpenOptions options);
    public Task<IReadOnlyList<IStorageFile>> OpenFilesAsync();
    public Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions options);
    public Task<IStorageFile?> SaveFileAsync();
    public Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options);
}

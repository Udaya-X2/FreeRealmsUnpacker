using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public interface IFilesService
{
    Task<IStorageFile?> OpenFileAsync();
    Task<IStorageFile?> OpenFileAsync(FilePickerOpenOptions options);
    Task<IReadOnlyList<IStorageFile>> OpenFilesAsync();
    Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions options);
    Task<IStorageFolder?> OpenFolderAsync();
    Task<IStorageFolder?> OpenFolderAsync(FolderPickerOpenOptions options);
    Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync();
    Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions options);
    Task<IStorageFile?> SaveFileAsync();
    Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options);
}

using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public interface IFilesService : IDisposable
{
    Task<IStorageFile?> OpenFileAsync() => OpenFileAsync(new());
    Task<IStorageFile?> OpenFileAsync(FilePickerOpenOptions options);
    Task<IReadOnlyList<IStorageFile>> OpenFilesAsync() => OpenFilesAsync(new() { AllowMultiple = true });
    Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions options);
    Task<IStorageFolder?> OpenFolderAsync() => OpenFolderAsync(new());
    Task<IStorageFolder?> OpenFolderAsync(FolderPickerOpenOptions options);
    Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync() => OpenFoldersAsync(new() { AllowMultiple = true });
    Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions options);
    Task<IStorageFile?> SaveFileAsync() => SaveFileAsync(new());
    Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options);
    Task<IStorageFolder?> TryGetFolderFromPathAsync(string folderPath);
    void DeleteOnExit(string filePath);
}

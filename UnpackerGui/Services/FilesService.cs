using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnpackerGui.Extensions;

namespace UnpackerGui.Services;

public class FilesService(TopLevel target) : IFilesService
{
    private readonly IStorageProvider _storageProvider = target.StorageProvider;
    private readonly List<string> _tempFolders = [];

    private bool _disposed;

    public async Task<IStorageFile?> OpenFileAsync(FilePickerOpenOptions options)
    {
        IReadOnlyList<IStorageFile> files = await OpenFilesAsync(options);
        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(FilePickerOpenOptions options)
    {
        using var _ = target.Disable();
        return await _storageProvider.OpenFilePickerAsync(options);
    }

    public async Task<IStorageFolder?> OpenFolderAsync(FolderPickerOpenOptions options)
    {
        IReadOnlyList<IStorageFolder> folders = await OpenFoldersAsync(options);
        return folders.Count >= 1 ? folders[0] : null;
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFoldersAsync(FolderPickerOpenOptions options)
    {
        using var _ = target.Disable();
        return await _storageProvider.OpenFolderPickerAsync(options);
    }

    public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options)
    {
        using var _ = target.Disable();
        return await _storageProvider.SaveFilePickerAsync(options);
    }

    public async Task<IStorageFolder?> TryGetFolderFromPathAsync(string folderPath)
    {
        using var _ = target.Disable();
        return await _storageProvider.TryGetFolderFromPathAsync(folderPath);
    }

    public string CreateTempFolder()
    {
        string folderPath = Directory.CreateTempSubdirectory("fru-").FullName;
        _tempFolders.Add(folderPath);
        return folderPath;
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (string folder in _tempFolders)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch
            {
            }
        }

        GC.SuppressFinalize(this);
        _disposed = true;
    }
}

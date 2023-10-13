using AssetIO;
using Avalonia.Platform.Storage;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using UnpackerGui.Services;
using UnpackerGui.Storage;

namespace UnpackerGui.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Assets = new ObservableCollection<Asset>();
        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
    }

    public ObservableCollection<Asset> Assets { get; }
    public ICommand AddPackFilesCommand { get; }

    private async Task AddPackFiles()
    {
        IFilesService filesService = App.Current?.Services?.GetService<IFilesService>()
            ?? throw new NullReferenceException("Missing file service instance.");
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = new[] { FilePickerTypes.PackFiles, FilePickerTypes.AllFiles }
        });

        foreach (IStorageFile file in files)
        {
            Assets.AddRange(ClientFile.GetPackAssets(file.Path.LocalPath));
        }
    }
}

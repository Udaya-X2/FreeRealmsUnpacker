using AssetIO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using UnpackerGui.Collections;
using UnpackerGui.Services;
using UnpackerGui.Storage;

namespace UnpackerGui.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Assets = new ReactiveList<Asset>();
        PackFiles = new ReactiveList<PackFileViewModel>();
        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
        RemoveSelectedCommand = ReactiveCommand.Create(RemoveSelected);
    }

    public ReactiveList<Asset> Assets { get; }
    public ReactiveList<PackFileViewModel> PackFiles { get; }
    public ICommand AddPackFilesCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand RemoveSelectedCommand { get; }

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
            string path = file.Path.LocalPath;

            if (PackFiles.All(x => x.Path != path))
            {
                PackFileViewModel packFile = new(path);
                packFile.PropertyChanged += UpdateAssets;
                PackFiles.Add(packFile);
            }
        }
    }

    private void UpdateAssets(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(PackFileViewModel.IsChecked)) return;

        PackFileViewModel packFile = sender as PackFileViewModel
            ?? throw new ArgumentException($"{nameof(sender)} must be a {nameof(PackFileViewModel)}.");

        if (packFile.IsChecked)
        {
            Assets.AddRange(packFile.Assets);
        }
        else if (Assets.Count > 0)
        {
            Assets.RemoveRange(packFile.Assets);
        }
    }

    private void SelectAll()
    {
        using (Assets.SuspendNotifications())
        {
            PackFiles.ForEach(x => x.IsChecked = true);
        }
    }

    private void DeselectAll()
    {
        Assets.Clear();
        PackFiles.ForEach(x => x.IsChecked = false);
    }

    private void RemoveSelected()
    {
        Assets.Clear();
        int index = 0;

        foreach (PackFileViewModel packFile in PackFiles.ToArray())
        {
            if (packFile.IsChecked)
            {
                packFile.IsChecked = false;
                PackFiles.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }
}

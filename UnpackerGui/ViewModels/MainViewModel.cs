using AssetIO;
using Avalonia.Platform.Storage;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using UnpackerGui.Services;
using UnpackerGui.Storage;

namespace UnpackerGui.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Assets = new List<Asset>();
        PackFiles = new ObservableCollection<PackFileViewModel>();
        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
        //Assets.CollectionChanged += (s, e) => Debug.WriteLine(Assets.Count);
    }

    public List<Asset> Assets { get; }
    public ObservableCollection<PackFileViewModel> PackFiles { get; }
    public ICommand AddPackFilesCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }

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
        else if (PackFiles.Count(x => x.IsChecked) > 1)
        {
            Assets.RemoveMany(packFile.Assets);
        }
        else
        {
            Assets.Clear();
        }
    }

    private void SelectAll()
    {
        foreach (PackFileViewModel packFile in PackFiles)
        {
            packFile.IsChecked = true;
        }
    }

    private void DeselectAll()
    {
        Assets.Clear();

        foreach (PackFileViewModel packFile in PackFiles)
        {
            packFile.IsChecked = false;
        }
    }
}

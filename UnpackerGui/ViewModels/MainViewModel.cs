using AssetIO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        SelectedAssets = new ReactiveList<Asset>();
        AssetFiles = new ReactiveList<AssetFileViewModel>();
#if DEBUG
        DebugCommand = ReactiveCommand.Create(() =>
        {
            string timestamp = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}]";
            Debug.WriteLine($"{timestamp} Assets.Count = {Assets.Count}");
            Debug.WriteLine($"{timestamp} SelectedAssets.Count = {SelectedAssets.Count}");
            Debug.WriteLine($"{timestamp} PackFiles.Count = {AssetFiles.Count}");
        });
#endif
        AddPackFilesCommand = ReactiveCommand.CreateFromTask(AddPackFiles);
        AddManifestFilesCommand = ReactiveCommand.CreateFromTask(AddManifestFiles);
        ExtractFilesCommand = ReactiveCommand.CreateFromTask(ExtractFiles);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
        RemoveSelectedCommand = ReactiveCommand.Create(RemoveSelected);
    }

    public ReactiveList<Asset> Assets { get; }
    public ReactiveList<Asset> SelectedAssets { get; }
    public ReactiveList<AssetFileViewModel> AssetFiles { get; }
    public ICommand DebugCommand { get; }
    public ICommand AddPackFilesCommand { get; }
    public ICommand AddManifestFilesCommand { get; }
    public ICommand ExtractFilesCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand RemoveSelectedCommand { get; }

    private async Task AddPackFiles()
        => await AddAssetFiles(AssetType.Game | AssetType.Pack, FileTypeFilters.PackFiles);

    private async Task AddManifestFiles()
        => await AddAssetFiles(AssetType.Game | AssetType.Dat, FileTypeFilters.DatFiles);

    private async Task AddAssetFiles(AssetType assetType, FilePickerFileType[] fileTypeFilter)
    {
        IFilesService filesService = App.Current?.Services?.GetService<IFilesService>()
            ?? throw new NullReferenceException("Missing file service instance.");
        IReadOnlyList<IStorageFile> files = await filesService.OpenFilesAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = fileTypeFilter
        });

        foreach (IStorageFile file in files)
        {
            string path = file.Path.LocalPath;

            if (AssetFiles.All(x => x.FullName != path))
            {
                AssetFileViewModel assetFile = new(new AssetFile(path, assetType));
                assetFile.PropertyChanged += UpdateAssets;
                AssetFiles.Add(assetFile);
            }
        }
    }

    private void UpdateAssets(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(AssetFileViewModel.IsChecked)) return;
        if (sender is not AssetFileViewModel assetFileVM) return;

        if (assetFileVM.IsChecked)
        {
            Assets.AddRange(assetFileVM.Assets);
        }
        else if (Assets.Count > 0)
        {
            // TODO: Remove based on asset file
            //Assets.RemoveAll(x => );
            Assets.RemoveRange(assetFileVM.Assets);
        }
    }

    private async Task ExtractFiles()
    {

    }

    private void SelectAll()
    {
        using (Assets.SuspendNotifications())
        {
            AssetFiles.ForEach(x => x.IsChecked = true);
        }
    }

    private void DeselectAll()
    {
        Assets.Clear();
        AssetFiles.ForEach(x => x.IsChecked = false);
    }

    private void RemoveSelected()
    {
        Assets.Clear();
        int index = 0;

        foreach (AssetFileViewModel assetFileVM in AssetFiles.ToArray())
        {
            if (assetFileVM.IsChecked)
            {
                assetFileVM.IsChecked = false;
                AssetFiles.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }
}

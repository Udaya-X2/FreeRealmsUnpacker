using AssetIO;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;

namespace UnpackerGui.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    public ReadOnlyObservableCollection<SettingsViewModel> Settings { get; }

    public ReactiveCommand<string, Unit> UpdateAssetFilterCommand { get; }

    private SettingsViewModel _selectedSetting;
    private FileConflictOptions _conflictOptions;
    private AssetType _assetFilter;
    private bool _addUnknownAssets;

    public PreferencesViewModel(MainViewModel mainViewModel)
    {
        Settings = new ReadOnlyObservableCollection<SettingsViewModel>(
        [
            new SettingsViewModel("File Conflict Options", "Select how to extract assets with conflicting names."),
            new SettingsViewModel("Folder Options", "Check the types of assets to add when opening a folder.")
        ]);
        UpdateAssetFilterCommand = ReactiveCommand.Create<string>(UpdateAssetFilter);
        _selectedSetting = Settings[0];
        _conflictOptions = mainViewModel.ConflictOptions;
        _assetFilter = mainViewModel.AssetFilter;
        _addUnknownAssets = mainViewModel.AddUnknownAssets;
        this.WhenAnyValue(x => x.ConflictOptions)
            .Subscribe(x => mainViewModel.ConflictOptions = x);
        this.WhenAnyValue(x => x.AssetFilter)
            .Subscribe(x => mainViewModel.AssetFilter = x);
        this.WhenAnyValue(x => x.AddUnknownAssets)
            .Subscribe(x => mainViewModel.AddUnknownAssets = x);
    }

    public SettingsViewModel SelectedSetting
    {
        get => _selectedSetting;
        set => this.RaiseAndSetIfChanged(ref _selectedSetting, value);
    }

    public FileConflictOptions ConflictOptions
    {
        get => _conflictOptions;
        set => this.RaiseAndSetIfChanged(ref _conflictOptions, value);
    }

    public AssetType AssetFilter
    {
        get => _assetFilter;
        set => this.RaiseAndSetIfChanged(ref _assetFilter, value);
    }

    public bool AddUnknownAssets
    {
        get => _addUnknownAssets;
        set => this.RaiseAndSetIfChanged(ref _addUnknownAssets, value);
    }

    private void UpdateAssetFilter(string value) => AssetFilter ^= Enum.Parse<AssetType>(value);
}

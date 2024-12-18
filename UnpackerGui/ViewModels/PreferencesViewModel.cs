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

    private readonly MainViewModel _mainViewModel;

    private SettingsViewModel _selectedSetting;

    public PreferencesViewModel(MainViewModel mainViewModel)
    {
        Settings = new ReadOnlyObservableCollection<SettingsViewModel>(
        [
            new SettingsViewModel("File Conflict Options", "Select how to extract assets with conflicting names."),
            new SettingsViewModel("Folder Options", "Check the types of assets to add when opening a folder.")
        ]);
        UpdateAssetFilterCommand = ReactiveCommand.Create<string>(UpdateAssetFilter);
        _mainViewModel = mainViewModel;
        _selectedSetting = Settings[0];
    }

    public SettingsViewModel SelectedSetting
    {
        get => _selectedSetting;
        set => this.RaiseAndSetIfChanged(ref _selectedSetting, value);
    }

    public FileConflictOptions ConflictOptions
    {
        get => _mainViewModel.ConflictOptions;
        set => _mainViewModel.ConflictOptions = value;
    }

    public AssetType AssetFilter
    {
        get => _mainViewModel.AssetFilter;
        set => _mainViewModel.AssetFilter = value;
    }

    public bool AddUnknownAssets
    {
        get => _mainViewModel.AddUnknownAssets;
        set => _mainViewModel.AddUnknownAssets = value;
    }

    private void UpdateAssetFilter(string value) => AssetFilter ^= Enum.Parse<AssetType>(value);
}

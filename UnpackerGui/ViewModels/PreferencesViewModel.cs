using AssetIO;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;

namespace UnpackerGui.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    public ReadOnlyObservableCollection<PreferenceViewModel> Preferences { get; }

    public ReactiveCommand<string, Unit> UpdateAssetFilterCommand { get; }

    private readonly MainViewModel _mainViewModel;

    private PreferenceViewModel _selectedPreference;

    public PreferencesViewModel(MainViewModel mainViewModel)
    {
        Preferences = new ReadOnlyObservableCollection<PreferenceViewModel>(
        [
            new PreferenceViewModel("File Conflict Options", "Select how to extract assets with conflicting names."),
            new PreferenceViewModel("Folder Options", "Check the types of assets to add when opening a folder.")
        ]);
        UpdateAssetFilterCommand = ReactiveCommand.Create<string>(UpdateAssetFilter);
        _mainViewModel = mainViewModel;
        _selectedPreference = Preferences[0];
    }

    public PreferenceViewModel SelectedPreference
    {
        get => _selectedPreference;
        set => this.RaiseAndSetIfChanged(ref _selectedPreference, value);
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

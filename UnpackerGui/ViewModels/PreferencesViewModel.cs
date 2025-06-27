using AssetIO;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using UnpackerGui.Config;

namespace UnpackerGui.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    public ReadOnlyObservableCollection<PreferenceViewModel> Preferences { get; }
    public ISettings Settings { get; }

    public ReactiveCommand<string, Unit> UpdateAssetFilterCommand { get; }

    private PreferenceViewModel _selectedPreference;

    public PreferencesViewModel()
    {
        Preferences = new ReadOnlyObservableCollection<PreferenceViewModel>(
        [
            new PreferenceViewModel("File Conflict Options", "Select how to extract assets with conflicting names."),
            new PreferenceViewModel("Folder Options", "Check the types of assets to add when opening a folder.")
        ]);
        UpdateAssetFilterCommand = ReactiveCommand.Create<string>(UpdateAssetFilter);
        _selectedPreference = Preferences[0];
        Settings = App.GetSettings();
    }

    public PreferenceViewModel SelectedPreference
    {
        get => _selectedPreference;
        set => this.RaiseAndSetIfChanged(ref _selectedPreference, value);
    }

    private void UpdateAssetFilter(string value) => Settings.AssetFilter ^= Enum.Parse<AssetType>(value);
}

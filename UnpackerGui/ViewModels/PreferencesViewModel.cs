using AssetIO;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reflection;

namespace UnpackerGui.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    public ReadOnlyObservableCollection<PreferenceViewModel> Preferences { get; }
    public IReadOnlyList<string> LineSeparators { get; }
    public SettingsViewModel Settings { get; }

    public ReactiveCommand<string, Unit> UpdateAssetFilterCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateSearchOptionCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSettingsCommand { get; }

    private PreferenceViewModel _selectedPreference;
    private int _selectedIndex;

    public PreferencesViewModel()
    {
        Preferences = new ReadOnlyObservableCollection<PreferenceViewModel>(
        [
            new PreferenceViewModel("File Conflict Options", "Select how to extract assets with conflicting names."),
            new PreferenceViewModel("Folder Options", "Check the types of assets to add when opening a folder."),
            new PreferenceViewModel("Appearance", "Customize the display settings."),
            new PreferenceViewModel("Miscellaneous", "Other options that don't fit the previous categories.")
        ]);
        LineSeparators = ["\r\n", "\n", "\r"];
        Settings = App.GetSettings();
        UpdateAssetFilterCommand = ReactiveCommand.Create<string>(UpdateAssetFilter);
        UpdateSearchOptionCommand = ReactiveCommand.Create(UpdateSearchOption);
        ResetSettingsCommand = ReactiveCommand.Create(ResetSettings);
        _selectedPreference = Preferences[0];
    }

    public PreferenceViewModel SelectedPreference
    {
        get => _selectedPreference;
        set => this.RaiseAndSetIfChanged(ref _selectedPreference, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
    }

    private void UpdateAssetFilter(string value) => Settings.AssetFilter ^= Enum.Parse<AssetType>(value);

    private void UpdateSearchOption() => Settings.SearchOption ^= SearchOption.AllDirectories;

    private void ResetSettings()
    {
        SettingsViewModel settings = new();
        const BindingFlags BindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;

        foreach (PropertyInfo property in Settings.GetType().GetProperties(BindingAttr))
        {
            property.SetValue(Settings, property.GetValue(settings));
        }
    }
}

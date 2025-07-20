using AssetIO;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reflection;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    public IReadOnlyList<Preference> Preferences { get; }
    public IReadOnlyList<string> LineSeparators { get; }

    public ReactiveCommand<string, Unit> UpdateAssetFilterCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateSearchOptionCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSettingsCommand { get; }

    private Preference _selectedPreference;
    private int _selectedIndex;

    public PreferencesViewModel()
    {
        Preferences =
        [
            new Preference("File Conflict Options", "Select how to extract assets with conflicting names."),
            new Preference("Folder Options", "Check the types of assets to add when opening a folder."),
            new Preference("Appearance", "Customize the display settings."),
            new Preference("Miscellaneous", "Other options that don't fit the previous categories.")
        ];
        LineSeparators = ["\r\n", "\n", "\r"];
        UpdateAssetFilterCommand = ReactiveCommand.Create<string>(UpdateAssetFilter);
        UpdateSearchOptionCommand = ReactiveCommand.Create(UpdateSearchOption);
        ResetSettingsCommand = ReactiveCommand.Create(ResetSettings);
        _selectedPreference = Preferences[0];
    }

    public Preference SelectedPreference
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

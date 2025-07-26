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
    /// <summary>
    /// Gets the preference categories.
    /// </summary>
    public static IReadOnlyList<Preference> Preferences { get; } =
    [
        new Preference("File Conflict Options", "Select how to extract assets with conflicting names."),
        new Preference("Folder Options", "Check the types of assets to add when opening a folder."),
        new Preference("Appearance", "Customize the display settings."),
        new Preference("Miscellaneous", "Other options that don't fit the previous categories.")
    ];
    
    /// <summary>
    /// Gets the available clipboard line separators.
    /// </summary>
    public static IReadOnlyList<string> LineSeparators { get; } = ["\r\n", "\n", "\r"];

    public ReactiveCommand<string, Unit> UpdateAssetFilterCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateSearchOptionCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSettingsCommand { get; }

    private Preference _selectedPreference;
    private int _selectedIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesViewModel"/> class.
    /// </summary>
    public PreferencesViewModel()
    {
        UpdateAssetFilterCommand = ReactiveCommand.Create<string>(UpdateAssetFilter);
        UpdateSearchOptionCommand = ReactiveCommand.Create(UpdateSearchOption);
        ResetSettingsCommand = ReactiveCommand.Create(ResetSettings);
        _selectedPreference = Preferences[0];
    }

    /// <summary>
    /// Gets or sets the selected preference.
    /// </summary>
    public Preference SelectedPreference
    {
        get => _selectedPreference;
        set => this.RaiseAndSetIfChanged(ref _selectedPreference, value);
    }

    /// <summary>
    /// Gets or sets the selected preference index.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
    }

    /// <summary>
    /// Toggles the specified asset type flag in the asset filter.
    /// </summary>
    private void UpdateAssetFilter(string value) => Settings.AssetFilter ^= Enum.Parse<AssetType>(value);

    /// <summary>
    /// Toggles between searching all directories recursively or only the top directory.
    /// </summary>
    private void UpdateSearchOption() => Settings.SearchOption ^= SearchOption.AllDirectories;

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
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

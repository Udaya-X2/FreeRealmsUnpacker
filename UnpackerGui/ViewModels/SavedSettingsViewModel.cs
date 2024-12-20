using AssetIO;
using DynamicData.Binding;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UnpackerGui.ViewModels;

/// <summary>
/// Represents the ViewModel properties that will be saved/loaded after the application is closed.
/// </summary>
public class SavedSettingsViewModel : ViewModelBase
{
    private bool _showName = true;
    private bool _showOffset = true;
    private bool? _showSize = true;
    private bool _showCrc32 = true;
    private FileConflictOptions _conflictOptions = FileConflictOptions.Overwrite;
    private AssetType _assetFilter = AssetType.All;
    private bool _addUnknownAssets = false;
    private string _inputDirectory = "";
    private string _outputDirectory = "";

    public SavedSettingsViewModel()
    {
        PropertyInfo[] properties = typeof(SavedSettingsViewModel).GetProperties(BindingFlags.Public
                                                                                 | BindingFlags.Instance
                                                                                 | BindingFlags.DeclaredOnly);

        // Read in the property values from the settings file.
        try
        {
            dynamic? settings = JsonConvert.DeserializeObject(File.ReadAllText("settings.json"));

            if (settings != null)
            {
                foreach (PropertyInfo property in properties)
                {
                    property.SetValue(this, Convert.ChangeType(settings[property.Name], property.PropertyType));
                }
            }
        }
        // If an error occurred, reset the settings file.
        catch
        {
            SaveSettings(this);
        }

        // Write the settings to a file whenever one of them change.
        this.WhenAnyPropertyChanged([.. properties.Select(x => x.Name)])
            .Subscribe(SaveSettings);
    }

    /// <summary>
    /// Gets or sets whether to show the asset's name.
    /// </summary>
    [JsonProperty]
    public bool ShowName
    {
        get => _showName;
        set => this.RaiseAndSetIfChanged(ref _showName, value);
    }

    /// <summary>
    /// Gets or sets whether to show the asset's offset.
    /// </summary>
    [JsonProperty]
    public bool ShowOffset
    {
        get => _showOffset;
        set => this.RaiseAndSetIfChanged(ref _showOffset, value);
    }

    /// <summary>
    /// Gets or sets whether to show the asset's size.
    /// </summary>
    [JsonProperty]
    public bool? ShowSize
    {
        get => _showSize;
        set => this.RaiseAndSetIfChanged(ref _showSize, value);
    }

    /// <summary>
    /// Gets or sets whether to show the asset's CRC-32.
    /// </summary>
    [JsonProperty]
    public bool ShowCrc32
    {
        get => _showCrc32;
        set => this.RaiseAndSetIfChanged(ref _showCrc32, value);
    }

    /// <summary>
    /// Gets or sets how to handle assets with conflicting names.
    /// </summary>
    [JsonProperty]
    public FileConflictOptions ConflictOptions
    {
        get => _conflictOptions;
        set => this.RaiseAndSetIfChanged(ref _conflictOptions, value);
    }

    /// <summary>
    /// Gets or sets which assets to search for in folders.
    /// </summary>
    [JsonProperty]
    public AssetType AssetFilter
    {
        get => _assetFilter;
        set => this.RaiseAndSetIfChanged(ref _assetFilter, value);
    }

    /// <summary>
    /// Gets or sets whether to search for unknown assets in folders.
    /// </summary>
    [JsonProperty]
    public bool AddUnknownAssets
    {
        get => _addUnknownAssets;
        set => this.RaiseAndSetIfChanged(ref _addUnknownAssets, value);
    }

    /// <summary>
    /// Gets or sets the default location to input files/folders.
    /// </summary>
    [JsonProperty]
    public string InputDirectory
    {
        get => _inputDirectory;
        set => this.RaiseAndSetIfChanged(ref _inputDirectory, value);
    }

    /// <summary>
    /// Gets or sets the default location to output files/folders.
    /// </summary>
    [JsonProperty]
    public string OutputDirectory
    {
        get => _outputDirectory;
        set => this.RaiseAndSetIfChanged(ref _outputDirectory, value);
    }

    /// <summary>
    /// Serializes the settings and writes it to a JSON file.
    /// </summary>
    private static void SaveSettings(SavedSettingsViewModel? settings)
        => File.WriteAllText("settings.json", JsonConvert.SerializeObject(settings, Formatting.Indented));
}

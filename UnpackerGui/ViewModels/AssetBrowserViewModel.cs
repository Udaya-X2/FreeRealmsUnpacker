using ReactiveUI;
using System;
using UnpackerGui.Collections;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public abstract class AssetBrowserViewModel : ViewModelBase
{
    private static readonly Lazy<SettingsViewModel> s_settings = new(App.GetSettings);

    private AssetInfo? _selectedAsset;

    /// <summary>
    /// Gets the selected assets.
    /// </summary>
    public ControlledObservableList SelectedAssets { get; } = [];

    /// <summary>
    /// Gets the assets shown to the user.
    /// </summary>
    public abstract FilteredReactiveCollection<AssetInfo> Assets { get; }

    /// <summary>
    /// Gets the application's settings.
    /// </summary>
    public static SettingsViewModel Settings => s_settings.Value;

    /// <summary>
    /// Gets or sets the selected asset.
    /// </summary>
    public AssetInfo? SelectedAsset
    {
        get => _selectedAsset;
        set => this.RaiseAndSetIfChanged(ref _selectedAsset, value);
    }
}

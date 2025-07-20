using DynamicData.Binding;
using ReactiveUI;
using System;
using UnpackerGui.Collections;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public abstract class AssetBrowserViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the application's settings.
    /// </summary>
    public static SettingsViewModel Settings => s_settings.Value;

    /// <summary>
    /// Gets the selected assets.
    /// </summary>
    public ControlledObservableList SelectedAssets { get; } = [];

    /// <summary>
    /// Gets the assets shown to the user.
    /// </summary>
    public abstract FilteredReactiveCollection<AssetInfo> Assets { get; }

    private static readonly Lazy<SettingsViewModel> s_settings = new(App.GetSettings);

    private AssetInfo? _selectedAsset;

    /// <summary>
    /// Gets or sets the selected asset.
    /// </summary>
    public AssetInfo? SelectedAsset
    {
        get => _selectedAsset;
        set => this.RaiseAndSetIfChanged(ref _selectedAsset, value);
    }

    /// <summary>
    /// Handles post asset initialization setup.
    /// </summary>
    protected virtual void OnAssetsInitialized()
    {
        // Need to clear selected assets to avoid the UI freezing when a large
        // number of assets are selected while more assets are added/removed.
        Assets.ObserveCollectionChanges()
              .Subscribe(_ => ClearSelectedAssets());
    }

    /// <summary>
    /// Clears the selected assets.
    /// </summary>
    protected virtual void ClearSelectedAssets()
    {
        SelectedAsset = null;
        SelectedAssets.Clear();
    }
}

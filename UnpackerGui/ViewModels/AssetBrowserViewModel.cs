using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Linq.Expressions;
using UnpackerGui.Collections;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public abstract class AssetBrowserViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the selected assets.
    /// </summary>
    public ControlledObservableList SelectedAssets { get; } = [];

    /// <summary>
    /// Gets the assets shown to the user.
    /// </summary>
    public abstract FilteredReactiveCollection<AssetInfo> Assets { get; }

    private AssetInfo? _selectedAsset;
    private IDisposable? _assetSuppressor;

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
    /// <param name="visibilitySetting">The visibility setting corresponding to this browser instance.</param>
    protected void OnAssetsInitialized(Expression<Func<SettingsViewModel, bool>>? visibilitySetting = null)
    {
        // Need to clear selected assets to avoid the UI freezing when a large
        // number of assets are selected while more assets are added/removed.
        Assets.ObserveCollectionChanges()
              .Subscribe(_ => ClearSelectedAssets());

        if (visibilitySetting == null) return;

        // Enable/disable asset updates depending on the visibility of the browser.
        Settings.WhenAnyValue(visibilitySetting)
                .Subscribe(isVisible =>
                {
                    if (isVisible)
                    {
                        _assetSuppressor?.Dispose();
                        _assetSuppressor = null;
                    }
                    else
                    {
                        _assetSuppressor ??= Assets.Disable();
                    }
                });
    }

    /// <summary>
    /// Clears the selected assets.
    /// </summary>
    protected void ClearSelectedAssets()
    {
        SelectedAsset = null;
        SelectedAssets.Clear();
    }
}

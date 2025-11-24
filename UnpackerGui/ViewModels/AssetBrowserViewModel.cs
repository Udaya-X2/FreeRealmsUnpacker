using DynamicData.Binding;
using ReactiveUI;
using System;
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

    private bool _isVisible;
    private AssetInfo? _selectedAsset;
    private IDisposable? _visibilityHandler;

    /// <summary>
    /// Gets or sets whether the browser is visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

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
    protected void OnAssetsInitialized(bool isMainBrowser = false)
    {
        // Need to clear selected assets to avoid the UI freezing when a large
        // number of assets are selected while more assets are added/removed.
        Assets.ObserveCollectionChanges()
              .Subscribe(_ => ClearSelectedAssets());

        if (isMainBrowser) return;

        // Enable/disable asset updates depending on the visibility of the browser.
        this.WhenAnyValue(x => x.IsVisible)
            .Subscribe(isVisible =>
            {
                _visibilityHandler?.Dispose();

                // Enable asset updates when the browser is visible.
                if (isVisible)
                {
                    _visibilityHandler = null;
                }
                // Disable asset updates the next time assets change when the browser is hidden.
                else
                {
                    _visibilityHandler = Assets.ObserveCollectionChanges()
                                               .Subscribe(x =>
                                               {
                                                   if (!_isVisible)
                                                   {
                                                       _visibilityHandler?.Dispose();
                                                       _visibilityHandler = Assets.Disable();
                                                   }
                                               });
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

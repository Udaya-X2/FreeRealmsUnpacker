using System;
using UnpackerGui.Collections;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public abstract class AssetBrowserViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the selected assets.
    /// </summary>
    public abstract ControlledObservableList SelectedAssets { get; }
    
    /// <summary>
    /// Gets the assets shown to the user.
    /// </summary>
    public abstract FilteredReactiveCollection<AssetInfo> Assets { get; }

    /// <summary>
    /// Gets the application's settings.
    /// </summary>
    public static SettingsViewModel Settings => _settings.Value;

    private static readonly Lazy<SettingsViewModel> _settings = new(App.GetSettings);
}

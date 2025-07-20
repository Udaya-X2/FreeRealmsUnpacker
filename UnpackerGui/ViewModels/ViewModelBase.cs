using ReactiveUI;
using System;

namespace UnpackerGui.ViewModels;

public class ViewModelBase : ReactiveObject
{
    private static readonly Lazy<SettingsViewModel> s_settings = new(App.GetSettings);

    /// <summary>
    /// Gets the application's settings.
    /// </summary>
    public static SettingsViewModel Settings => s_settings.Value;
}

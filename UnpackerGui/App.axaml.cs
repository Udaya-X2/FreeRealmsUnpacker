using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using UnpackerGui.Services;
using UnpackerGui.ViewModels;
using UnpackerGui.Views;

namespace UnpackerGui;

public partial class App : Application
{
    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public static new App? Current => Application.Current as App;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider? Services { get; private set; }

    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Performs control-specific initialization tasks.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            Services = new ServiceCollection().AddSingleton<IFilesService>(new FilesService(desktop.MainWindow))
                                              .AddSingleton<IDialogService>(new DialogService(desktop.MainWindow))
                                              .BuildServiceProvider();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

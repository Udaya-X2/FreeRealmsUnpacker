using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
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
        if (Design.IsDesignMode)
        {
            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Returns the service of type <typeparamref name="T"/> from
    /// the current application's <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <returns>The service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.</returns>
    /// <exception cref="InvalidOperationException"/>
    public static T GetService<T>() where T : class
        => Current?.Services?.GetService<T>()
        ?? throw new InvalidOperationException($"Missing {typeof(T).Name} instance.");

    /// <summary>
    /// Displays an error dialog with the specified exception.
    /// </summary>
    public static async Task ShowErrorDialog(Exception ex, bool handled)
    {
        await GetService<IDialogService>().ShowDialog(new ErrorWindow
        {
            DataContext = new ErrorViewModel(ex, handled)
        });
    }
}

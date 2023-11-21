using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading;
using UnpackerGui.Collections;
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
            AppDomain.CurrentDomain.UnhandledException += OnFatalException;
            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(OnRecoverableException);
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
    /// Displays a fatal error message with the specified exception.
    /// </summary>
    private static void OnFatalException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception) return;
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        // Disable all open windows.
        desktop.Windows.ForEach(x => x.IsEnabled = false);

        // Shut down the application after the error dialog is closed.
        using CancellationTokenSource cts = new();
        GetService<IDialogService>().ShowErrorDialog(exception, unhandled: true)
                                    .ContinueWith(x =>
                                    {
                                        // Hide all open windows prior to shutdown.
                                        Dispatcher.UIThread.Invoke(() => desktop.Windows.ForEach(x => x.Hide()));
                                        cts.Cancel();
                                    });
        Dispatcher.UIThread.MainLoop(cts.Token);
    }

    /// <summary>
    /// Displays an error message with the specified exception.
    /// </summary>
    private static async void OnRecoverableException(Exception exception)
        => await GetService<IDialogService>().ShowErrorDialog(exception);
}

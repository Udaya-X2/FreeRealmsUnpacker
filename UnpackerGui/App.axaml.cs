using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Config.Net;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnpackerGui.Collections;
using UnpackerGui.Config;
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

    /// <summary>
    /// Gets the application's clipboard implementation.
    /// </summary>
    public IClipboard? Clipboard { get; private set; }

    /// <summary>
    /// Gets the application's settings.
    /// </summary>
    public ISettings? Settings { get; private set; }

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
            Settings = ReadSettings();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            FilesService filesService = new(desktop.MainWindow);
            DialogService dialogService = new(desktop.MainWindow);
            Services = new ServiceCollection().AddSingleton<IFilesService>(filesService)
                                              .AddSingleton<IDialogService>(dialogService)
                                              .BuildServiceProvider();
            Clipboard = desktop.MainWindow.Clipboard;
            desktop.Exit += (s, e) => filesService.Dispose();
        }
        if (Design.IsDesignMode)
        {
            RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            Settings = new ConfigurationBuilder<ISettings>().Build();
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
    /// Sets the current application's clipboard text to the specified value.
    /// </summary>
    public static async Task SetClipboardText(string? text)
    {
        if (Current?.Clipboard?.SetTextAsync(text) is Task task)
        {
            await task;
        }
    }

    /// <summary>
    /// Gets the current application's settings.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static ISettings GetSettings()
        => Current?.Settings ?? throw new InvalidOperationException("Application settings not initialized.");

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    public static void ShutDown()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.TryShutdown();
        }
    }

    /// <summary>
    /// Displays a fatal error message with the specified exception.
    /// </summary>
    private static void OnFatalException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception) return;
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        // If no windows are open, create a crash log file instead.
        if (desktop.Windows.Count == 0)
        {
            File.WriteAllText($"crash_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log", exception.ToString());
            return;
        }

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

    /// <summary>
    /// Reads the application's settings from the settings file.
    /// </summary>
    /// <returns>The application settings.</returns>
    private static ISettings ReadSettings()
    {
        const string SettingsFile = "settings.json";

        try
        {
            return new ConfigurationBuilder<ISettings>().UseJsonFile(SettingsFile)
                                                        .Build();
        }
        catch (JsonException)
        {
            File.Delete(SettingsFile);
            return new ConfigurationBuilder<ISettings>().UseJsonFile(SettingsFile)
                                                        .Build();
        }
    }
}

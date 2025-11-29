using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnpackerGui.Collections;
using UnpackerGui.Enums;
using UnpackerGui.Services;
using UnpackerGui.ViewModels;
using UnpackerGui.Views;

namespace UnpackerGui;

/// <inheritdoc/>
public partial class App : Application
{
    private static readonly string s_settingsFile = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

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
    public SettingsViewModel? Settings { get; private set; }

    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Handle exceptions gracefully via error dialogs, if possible.
            AppDomain.CurrentDomain.UnhandledException += OnFatalException;
            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(OnRecoverableException);

            // Read the settings file.
            DataContext = ReadSettings();

            // Create the main window.
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            // Set up application services.
            FilesService filesService = new(desktop.MainWindow);
            DialogService dialogService = new(desktop.MainWindow);
            Services = new ServiceCollection().AddSingleton<IFilesService>(filesService)
                                              .AddSingleton<IDialogService>(dialogService)
                                              .BuildServiceProvider();
            Clipboard = desktop.MainWindow.Clipboard;

            // Release temporary files and save settings on exit.
            desktop.Exit += (s, e) =>
            {
                filesService.Dispose();
                SaveSettings();
            };
        }
        else if (Design.IsDesignMode)
        {
            // Use the default settings in design mode.
            DataContext = Settings = new SettingsViewModel() { ColorTheme = ColorTheme.Light };
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        // Update the application's theme when requested.
        Settings.WhenAnyValue(x => x.ColorTheme)
                .Subscribe(SetTheme);
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
    /// Sets the current application's clipboard data to the specified value.
    /// </summary>
    public static async Task SetClipboardData(string id, byte[] value)
    {
        DataTransfer dataTransfer = new();
        DataTransferItem dataTransferItem = new();
        dataTransferItem.Set(DataFormat.CreateBytesPlatformFormat(id), value);
        dataTransfer.Add(dataTransferItem);

        if (Current?.Clipboard?.SetDataAsync(dataTransfer) is Task task)
        {
            await task;
        }
    }

    /// <summary>
    /// Gets the current application's settings.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public static SettingsViewModel GetSettings()
        => Current?.Settings ?? throw new InvalidOperationException("Application settings not initialized.");

    /// <summary>
    /// Sets the current application's theme to the specified value.
    /// </summary>
    public static void SetTheme(ColorTheme theme) => Current!.RequestedThemeVariant = theme switch
    {
        ColorTheme.Dark => ThemeVariant.Dark,
        ColorTheme.Light => ThemeVariant.Light,
        ColorTheme.SystemDefault => ThemeVariant.Default,
        _ => throw new ArgumentOutOfRangeException(nameof(theme), SR.ArgumentOutOfRange_Enum)
    };

    /// <summary>
    /// Tries to get the application resource with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the resource</typeparam>
    /// <returns>The application resource.</returns>
    /// <exception cref="InvalidCastException"/>
    public static bool TryGetResource<T>(object key, [NotNullWhen(true)] out T? value)
    {
        if (Current?.TryGetResource(key, Current.ActualThemeVariant, out object? resource) ?? false)
        {
            value = (T)resource!;
            return true;
        }

        value = default;
        return false;
    }

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
    private SettingsViewModel ReadSettings()
    {
        try
        {
            using FileStream stream = File.OpenRead(s_settingsFile);
            Settings = JsonSerializer.Deserialize<SettingsViewModel>(stream, s_jsonOptions);
        }
        catch
        {
        }

        return Settings ??= new SettingsViewModel();
    }

    /// <summary>
    /// Saves the application's settings to the settings file.
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            using FileStream fs = File.Open(s_settingsFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(fs, Settings, s_jsonOptions);
        }
        catch
        {
        }
    }
}

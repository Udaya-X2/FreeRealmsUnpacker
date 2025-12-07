using Avalonia;
using ManagedBass;
using ReactiveUI.Avalonia;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnpackerGui;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly, ResolveBassLibrary);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    /// <summary>
    /// Loads the BASS library corresponding to the current runtime.
    /// </summary>
    private static nint ResolveBassLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        try
        {
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }
        catch
        {
            string suffix = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

            if (OperatingSystem.IsWindows()) libraryName = $"runtimes/win-{suffix}/native/{libraryName}.dll";
            else if (OperatingSystem.IsLinux()) libraryName = $"runtimes/linux-{suffix}/native/lib{libraryName}.so";
            else if (OperatingSystem.IsMacOS()) libraryName = $"runtimes/osx/native/lib{libraryName}.dylib";
            else throw;

            return NativeLibrary.Load(libraryName, assembly, DllImportSearchPath.ApplicationDirectory);
        }
    }
}

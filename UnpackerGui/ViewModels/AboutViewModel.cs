using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnpackerGui.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public virtual string Version { get; }
    public virtual string Copyright { get; }
    public virtual string SourceCodeUrl { get; }

    public ReactiveCommand<string, Unit> OpenLinkCommand { get; }

    public AboutViewModel()
    {
        Assembly assembly = Assembly.GetEntryAssembly()!;
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Version = $"Version {version}";
        Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()!.Copyright;
        SourceCodeUrl = "https://github.com/Udaya-X2/FreeRealmsUnpacker";
        OpenLinkCommand = ReactiveCommand.Create<string>(OpenLink);
    }

    /// <summary>
    /// Opens the default browser to the specified URI.
    /// </summary>
    /// <remarks>
    /// Source: <see href="https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/"/>
    /// </remarks>
    private static void OpenLink(string uri)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            uri = uri.Replace("&", "^&");
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", uri);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", uri);
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }
}

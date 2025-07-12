using System.Reflection;

namespace UnpackerGui.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public virtual string Version { get; }
    public virtual string Copyright { get; }
    public virtual string SourceCodeUrl { get; }

    public AboutViewModel()
    {
        Assembly assembly = Assembly.GetEntryAssembly()!;
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Version = $"Version {version}";
        Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()!.Copyright;
        SourceCodeUrl = "https://github.com/Udaya-X2/FreeRealmsUnpacker";
    }
}

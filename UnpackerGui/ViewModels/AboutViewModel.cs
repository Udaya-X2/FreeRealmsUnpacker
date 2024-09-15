using System.Reflection;

namespace UnpackerGui.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public static string Version => $"Version {Assembly.GetEntryAssembly()
                                                      ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                                      ?.InformationalVersion}";
}

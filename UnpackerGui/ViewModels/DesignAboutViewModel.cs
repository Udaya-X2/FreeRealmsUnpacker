namespace UnpackerGui.ViewModels;

public class DesignAboutViewModel : AboutViewModel
{
    public override string Version { get; }
    public override string Copyright { get; }
    public override string SourceCodeUrl { get; }

    public DesignAboutViewModel()
    {
        Version = "Version 1.0.0";
        Copyright = "Copyright © Udaya";
        SourceCodeUrl = "https://github.com/Udaya-X2/FreeRealmsUnpacker";
    }
}

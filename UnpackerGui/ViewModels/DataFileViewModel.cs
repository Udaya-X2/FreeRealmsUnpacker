using System.IO;

namespace UnpackerGui.ViewModels;

public class DataFileViewModel(string path, AssetFileViewModel assetFile) : ViewModelBase
{
    public AssetFileViewModel Parent { get; } = assetFile;
    public FileInfo Info { get; } = new(path);
    public string FullName => Info.FullName;
    public string Name => Info.Name;
    public string? DirectoryName => Info.DirectoryName;
}

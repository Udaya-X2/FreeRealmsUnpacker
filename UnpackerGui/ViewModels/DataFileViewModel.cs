using System.IO;

namespace UnpackerGui.ViewModels;

public class DataFileViewModel(string path, AssetFileViewModel assetFile) : ViewModelBase
{
    public AssetFileViewModel Parent { get; } = assetFile;
    public string FullName { get; } = path;
    public string Name { get; } = Path.GetFileName(path);
}

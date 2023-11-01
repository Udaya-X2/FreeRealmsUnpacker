using System.IO;

namespace UnpackerGui.ViewModels;

public class DataFileViewModel : AssetFileViewModel
{
    public AssetFileViewModel Parent { get; }
    public override string FullName { get; }
    public override string Name { get; }

    public DataFileViewModel(string path, AssetFileViewModel assetFile)
        : base(assetFile)
    {
        Parent = assetFile;
        FullName = path;
        Name = Path.GetFileName(path);
    }
}

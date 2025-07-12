using AssetIO;

namespace UnpackerGui.ViewModels;

public class DesignAssetFileViewModel : AssetFileViewModel
{
    public DesignAssetFileViewModel()
        : base(CreateAssetFile())
    {
        Delete();
    }

    private static AssetFile CreateAssetFile()
    {
        AssetFile assetFile = new("Assets_000.pack");
        assetFile.Create();
        return assetFile;
    }
}

using System.Collections.Generic;

namespace UnpackerGui.ViewModels;

public class ExtractionViewModel : ViewModelBase
{
    private readonly string _outputDir;
    private readonly IList<AssetFileViewModel> _assetFiles;

    public ExtractionViewModel(string outputDir, IList<AssetFileViewModel> assetFiles)
    {
        _outputDir = outputDir;
        _assetFiles = assetFiles;
    }
}

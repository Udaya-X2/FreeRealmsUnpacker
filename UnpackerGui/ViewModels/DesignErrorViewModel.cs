using AssetIO;
using System.IO;

namespace UnpackerGui.ViewModels;

public class DesignErrorViewModel : ErrorViewModel
{
    public DesignErrorViewModel()
    {
        try
        {
            new AssetDatReader([]).Read(new Asset("profileicon.png", 0, 826, 1223794746));
        }
        catch (IOException ex)
        {
            Exception = ex;
        }

        Handled = true;
    }
}

using System.Linq;

namespace UnpackerGui.ViewModels;

public class DesignHexBrowserViewModel : HexBrowserViewModel
{
    public DesignHexBrowserViewModel() : base(new DesignMainViewModel().Assets)
    {
        SelectedAsset = Assets.FirstOrDefault();
    }
}

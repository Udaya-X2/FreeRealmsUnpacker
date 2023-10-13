using AssetIO;
using System.Collections.ObjectModel;

namespace UnpackerGui.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<Asset> Assets { get; } = new(ClientFile.GetPackAssets(@"C:\Users\udaya\Downloads\Temp\shared\Assets_000.pack"));
}

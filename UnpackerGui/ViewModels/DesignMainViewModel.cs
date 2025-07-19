using AssetIO;
using System.IO;
using System.Linq;
using UnpackerGui.Collections;

namespace UnpackerGui.ViewModels;

public class DesignMainViewModel : MainViewModel
{
    public DesignMainViewModel()
    {
        // TODO: Update sample design files
        if (Directory.Exists(@"C:\Users\udaya\Downloads\Temp\tmp"))
        {
            foreach (AssetFile assetFile in ClientDirectory.EnumerateAssetFiles(@"C:\Users\udaya\Downloads\Temp\tmp"))
            {
                AddCheckedFile(assetFile.FullName);
            }
        }
        else
        {
            AssetFile[] assetFiles =
            [
                new AssetFile("Assets_000.pack"),
                new AssetFile("Assets_001.pack"),
                new AssetFile("Assets_002.pack"),
                new AssetFile("Assets_manifest.dat")
            ];
            Asset[] assets =
            [
                new Asset("loadingscreen_pirAB_black_bbe.lst", 8192, 232, 917176159),
                new Asset("loadingscreen_pirSC_purple_bbe.lst", 8424, 234, 283437766),
                new Asset("img9325829751207516497.dds", 8658, 21972, 4028054915),
                new Asset("game_checkers.lst", 30630, 413, 2723755226),
                new Asset("img9733414106754042705.dds", 31043, 2896, 3333931974),
                new Asset("img14705584953516941364.dds", 33939, 2172, 576028079),
                new Asset("img14945388600970024253.dds", 36111, 2172, 2903660400),
                new Asset("img1722132450021161281.dds", 38283, 2172, 3390588015),
                new Asset("img3668920196757216193.dds", 40455, 2172, 4257021593),
                new Asset("img18161411433257056168.dds", 42627, 21972, 77484070),
                new Asset("img17181553199797448350.dds", 64599, 2172, 4191555634),
                new Asset("img106938439149170106.dds", 66771, 2172, 2655596961),
                new Asset("img612150743518879743.dds", 68943, 2172, 367545122),
                new Asset("img14394140785298774038.dds", 71115, 2172, 2986800891),
                new Asset("img6031978252652807697.dds", 73287, 2172, 1891024886),
                new Asset("img6059153550170124511.dds", 75459, 1520, 3689728656),
                new Asset("img10217450707051872979.dds", 76979, 1512, 2148950447),
                new Asset("img9267049453292051791.dds", 78491, 824, 2979881099),
                new Asset("ForcePerception_Stage10.lst", 79315, 901, 2760657746),
                new Asset("img13342767942319076288.dds", 80216, 824, 3420530212),
                new Asset("img13081608134709798931.dds", 81040, 1512, 3231138746),
                new Asset("img13834063873837893418.dds", 82552, 824, 2581018384),
                new Asset("img6989632132912378573.dds", 83376, 1512, 3898984097),
                new Asset("img756135788043379202.dds", 84888, 824, 1831312818),
                new Asset("img12588569643475886281.dds", 85712, 824, 2877080669),
                new Asset("img4115401578639383289.dds", 86536, 1512, 338448729),
                new Asset("img16290774806609951400.dds", 88048, 824, 1897712786),
                new Asset("img16449769463892741406.dds", 88872, 824, 1238894570)
            ];
            byte[] data = new byte[assets.Max(x => x.Size)];
            int n = assets.Length / assetFiles.Length;
            int j = 0;

            foreach (AssetFile assetFile in assetFiles)
            {
                try
                {
                    using (AssetWriter writer = assetFile.OpenWrite())
                    {
                        for (int i = 0; i < n; i++, j++)
                        {
                            Asset asset = assets[j];
                            writer.Write(asset.Name, data, 0, (int)asset.Size);
                        }
                    }

                    AddCheckedFile(assetFile.FullName);
                    Settings.RecentFiles.Add(assetFile.FullName);
                    Settings.RecentFolders.Add(assetFile.DirectoryName!);
                }
                finally
                {
                    assetFile.Info.Delete();
                    assetFile.DataFiles.ForEach(File.Delete);
                }
            }
        }

        SelectedAssetFile = AssetFiles.FirstOrDefault();
    }
}

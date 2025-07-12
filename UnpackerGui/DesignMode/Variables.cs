using AssetIO;
using Avalonia.Controls;
using System.Linq;
using UnpackerGui.Models;
using UnpackerGui.ViewModels;

namespace UnpackerGui.DesignMode;

public static class Variables
{
    /* MainWindow Design Variables */
    public static readonly AssetFileViewModel[]? AssetFiles;
    public static readonly AssetInfo[]? Assets;
    public const string UnknownPath = "???";
    public const string PropertyGridPath = UnknownPath;
    public const string PropertyGridCount = "-1";
    public const string PropertyGridSize = "-1 KB";

    /* Generic Design Variables */
    public const bool True = true;
    public const bool False = false;

    /* Static Constructor */
    static Variables()
    {
        if (Design.IsDesignMode)
        {
            AssetFile[] assetFiles =
            [
                new AssetFile("Assets_001.pack"),
                new AssetFile("Assets_002.pack"),
                new AssetFile("Assets_003.pack"),
                new AssetFile("Assets_004.pack"),
                new AssetFile("Assets_005.pack"),
                new AssetFile("Assets_manifest.dat")
            ];
            AssetFiles = [.. assetFiles.Select(x => new AssetFileViewModel(x))];
            Assets =
            [
                new AssetInfo("loadingscreen_piratesplunder_es_MX.lst", 8192, 413, 1428684137, assetFiles[0]),
                new AssetInfo("loadingscreen_pirSC_orange_bbe.lst", 8605, 235, 1203721438, assetFiles[0]),
                new AssetInfo("loadscreen_seaside.lst", 8840, 381, 617394089, assetFiles[0]),
                new AssetInfo("img15569720312116940423.dds", 9221, 22000, 564079490, assetFiles[0]),
                new AssetInfo("img18309017131444767154.dds", 31221, 5616, 3556937442, assetFiles[0]),
                new AssetInfo("tutorial_checkers.lst", 36837, 339, 3267925406, assetFiles[0]),
                new AssetInfo("img8118518776976251266.dds", 37176, 2172, 3232705228, assetFiles[0]),
                new AssetInfo("img11271709678882680838.dds", 39348, 2172, 2065150787, assetFiles[0]),
                new AssetInfo("img5865697909579607231.dds", 41520, 2172, 348218531, assetFiles[0]),
                new AssetInfo("img9858373679194077357.dds", 43692, 2172, 509118986, assetFiles[0]),
            ];
        }
    }
}

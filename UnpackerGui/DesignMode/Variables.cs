using AssetIO;
using Avalonia.Controls;
using UnpackerGui.ViewModels;

namespace UnpackerGui.DesignMode;

public static class Variables
{
    /* MainWindow Design Variables */
    public static readonly AssetFileViewModel[]? AssetFiles;

    /* ProgressWindow Design Variables */
    public const string ProgressMessage = "Progress Message";
    public const string ElapsedTime = "00:00:00";
    public const int ProgressValue = 50;

    /* ErrorWindow Design Variables */
    public const string ErrorMessage = "An error has occurred.\n\nRan out of asset .dat files while reading 'profile" +
        "icon.png'.";
    public const string DetailsMessage = "System.IO.IOException: Ran out of asset .dat files while reading 'profilei" +
        "con.png'.\n   at AssetIO.AssetDatReader.GetAssetStream(Int64 file, Asset asset) in C:\\GitHub\\FreeRealmsUn" +
        "packer\\AssetIO\\AssetDatReader.cs:line 269\n   at AssetIO.AssetDatReader.InternalRead(Asset asset)+MoveNex" +
        "t() in C:\\GitHub\\FreeRealmsUnpacker\\AssetIO\\AssetDatReader.cs:line 194\n   at AssetIO.AssetDatReader.Co" +
        "pyTo(Asset asset, Stream destination) in C:\\GitHub\\FreeRealmsUnpacker\\AssetIO\\AssetDatReader.cs:line 75";
    public const int FlipImageAngle = 180;

    /* PreferencesWindow Design Variables */
    public static readonly string[]? Preferences;
    public const string PreferenceName = "File Conflict Options";
    public const string PreferenceDescription = "Select how to extract assets with conflicting names.";

    /* AboutWindow Design Variables */
    public const string Version = "Version X.X.X";
    public const string Copyright = "Copyright © Udaya";

    /* Generic Design Variables */
    public const bool True = true;
    public const bool False = false;

    /* Static Constructor */
    static Variables()
    {
        if (Design.IsDesignMode)
        {
            AssetFiles =
            [
                new AssetFileViewModel(new AssetFile("Assets_001.pack")),
                new AssetFileViewModel(new AssetFile("Assets_002.pack")),
                new AssetFileViewModel(new AssetFile("Assets_003.pack")),
                new AssetFileViewModel(new AssetFile("Assets_004.pack")),
                new AssetFileViewModel(new AssetFile("Assets_005.pack")),
                new AssetFileViewModel(new AssetFile("Assets_manifest.dat"))
            ];
            Preferences =
            [
                "File Conflict Options",
                "Folder Options"
            ];
        }
    }
}

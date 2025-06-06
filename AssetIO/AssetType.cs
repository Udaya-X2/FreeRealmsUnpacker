﻿namespace AssetIO;

/// <summary>
/// Specifies the types of Free Realms assets.
/// </summary>
[Flags]
public enum AssetType
{
    /// <summary>
    /// Assets for the game, typically located in the "Free Realms/" directory.
    /// </summary>
    Game = 1,
    /// <summary>
    /// Image assets for the TCG, typically located in the "Free Realms/assets/" directory.
    /// </summary>
    Tcg = 2,
    /// <summary>
    /// Resource assets for the TCG, typically located in the "Free Realms/tcg/" directory.
    /// </summary>
    Resource = 4,
    /// <summary>
    /// Assets for the PS3 version of the game, typically located
    /// in the "NPUA30048/USRDIR/" or "NPEA00299/USRDIR/" directory.
    /// </summary>
    PS3 = 8,
    /// <summary>
    /// Asset .pack files, which consist of both asset information and content.
    /// </summary>
    Pack = 16,
    /// <summary>
    /// Asset .dat files, which come with a manifest.dat file consisting of asset information.
    /// </summary>
    Dat = 32,
    /// <summary>
    /// Asset .temp files, which retain a portion of the original asset data.
    /// </summary>
    Temp = 64,
    /// <summary>
    /// Assets from any Free Realms client subdirectory.
    /// </summary>
    AllDirectories = Game | Tcg | Resource | PS3,
    /// <summary>
    /// Asset files with any file extension.
    /// </summary>
    AllFiles = Pack | Dat | Temp,
    /// <summary>
    /// Assets from any Free Realms client subdirectory, with any file extension.
    /// </summary>
    All = AllDirectories | AllFiles
}
